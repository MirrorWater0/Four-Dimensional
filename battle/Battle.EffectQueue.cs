using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class Battle {
    [ExportGroup("Effect Queue")]
    [Export(PropertyHint.Range, "1,8,1")]
    public int EffectsPerFrame { get; set; } = 1;

    private sealed class BattleEffectWork {
        public readonly Func<Task> Execute;
        public readonly string Name;
        public readonly bool ConsumeFrameBudget;
        public readonly TaskCompletionSource<bool> Completion = new();

        public BattleEffectWork(Func<Task> execute, string name, bool consumeFrameBudget = true) {
            Execute = execute;
            Name = name;
            ConsumeFrameBudget = consumeFrameBudget;
        }
    }

    private sealed class BattleEffectScope {
        public readonly Queue<BattleEffectWork> Pending = new();
        public bool Active = true;
        public bool Draining;
    }

    // A global "is executing" flag would also let unrelated UI callbacks bypass the queue.
    // Only the current async call chain may execute awaited child effects in place.
    private readonly AsyncLocal<BattleEffectScope> _battleEffectScope = new();
    private readonly Queue<BattleEffectWork> _battleEffects = new();
    private readonly TaskCompletionSource<bool> _battleEffectsStopped = new();
    private BattleEffectWork _activeBattleEffect;
    private bool _processingBattleEffects;
    private ulong _battleEffectFrame;
    private int _battleEffectsStartedThisFrame;
    private int _deferredDyingResolutionDepth;
    private readonly List<(Character Target, Character Source)> _deferredDyingTargets = new();

    public bool IsExecutingBattleEffects => _processingBattleEffects || _battleEffects.Count > 0;
    internal bool IsDeferringDyingResolution => _deferredDyingResolutionDepth > 0;
    private bool CanExecuteBattleEffects => !_lifetimeCts.IsCancellationRequested
        && GodotObject.IsInstanceValid(this) && IsInsideTree() && !_retreating;
    private BattleEffectScope CurrentBattleEffectScope =>
        _battleEffectScope.Value is { Active: true } scope ? scope : null;

    internal void BeginDeferredDyingResolution() {
        _deferredDyingResolutionDepth++;
    }

    internal void DeferDyingResolution(Character target, Character source) {
        if (!IsDeferringDyingResolution || target == null || !GodotObject.IsInstanceValid(target))
            return;

        target.MarkDyingResolutionDeferred(source);
        if (!_deferredDyingTargets.Any(entry => ReferenceEquals(entry.Target, target)))
            _deferredDyingTargets.Add((target, source));
    }

    internal async Task EndDeferredDyingResolutionAsync() {
        if (_deferredDyingResolutionDepth <= 0 || --_deferredDyingResolutionDepth > 0)
            return;

        while (_deferredDyingTargets.Count > 0) {
            var pending = _deferredDyingTargets.ToArray();
            _deferredDyingTargets.Clear();
            foreach (var entry in pending) {
                if (entry.Target == null || !GodotObject.IsInstanceValid(entry.Target)
                    || entry.Target.State != Character.CharacterState.Dying)
                    continue;
                await entry.Target.Dying(entry.Source);
            }
        }
    }

    public async Task ExecuteBattleEffectAsync(Func<Task> effect, string name = null,
        bool consumeFrameBudget = true) {
        if (effect == null || !CanExecuteBattleEffects)
            return;

        var work = new BattleEffectWork(effect, name, consumeFrameBudget);
        BattleEffectScope scope = CurrentBattleEffectScope;
        if (scope != null) {
            // On-play and other synchronous event handlers finish before the next plan step.
            await DrainBattleEffectScopeAsync(scope);
            await RunBattleEffectAsync(work);
        }
        else {
            _battleEffects.Enqueue(work);
            _ = ProcessBattleEffectsAsync();
            await work.Completion.Task;
        }
    }

    // Event handlers enqueue factories, never already-started Tasks.
    public void QueueBattleEffect(Func<Task> effect, string name = null,
        bool consumeFrameBudget = true) {
        if (effect == null || !CanExecuteBattleEffects)
            return;

        var work = new BattleEffectWork(effect, name, consumeFrameBudget);
        BattleEffectScope scope = CurrentBattleEffectScope;
        if (scope != null)
            scope.Pending.Enqueue(work);
        else {
            _battleEffects.Enqueue(work);
            _ = ProcessBattleEffectsAsync();
        }
    }

    public async Task ResolvePendingBattleEffectsAsync() {
        BattleEffectScope scope = CurrentBattleEffectScope;
        if (scope != null) {
            await DrainBattleEffectScopeAsync(scope);
            return;
        }

        while (CanExecuteBattleEffects && IsExecutingBattleEffects)
            await WaitForBattleEffectFrameAsync();
    }

    private async Task ProcessBattleEffectsAsync() {
        if (_processingBattleEffects)
            return;

        _processingBattleEffects = true;
        try {
            await WaitForBattleEffectFrameAsync();
            while (CanExecuteBattleEffects && _battleEffects.Count > 0) {
                BattleEffectWork work = _battleEffects.Dequeue();
                _activeBattleEffect = work;
                try {
                    await RunBattleEffectAsync(work);
                    work.Completion.TrySetResult(true);
                }
                catch (Exception ex) {
                    // Awaited callers receive the failure; event-only work is still reported.
                    GD.PushError($"Battle effect '{work.Name ?? "unnamed"}' failed: {ex}");
                    work.Completion.TrySetException(ex);
                    _ = work.Completion.Task.Exception;
                }
                finally {
                    _activeBattleEffect = null;
                }
            }
        }
        finally {
            _processingBattleEffects = false;
            if (!CanExecuteBattleEffects)
                CancelBattleEffects();
            else if (_battleOverCheckPendingAfterEffect && !HasEffectSourceContext) {
                _battleOverCheckPendingAfterEffect = false;
                QueueBattleOverCheck();
            }
        }
    }

    private async Task RunBattleEffectAsync(BattleEffectWork work) {
        if (!CanExecuteBattleEffects)
            return;

        // Only gameplay work consumes the budget; orchestration must not add a frame
        // at every nesting level. Presentation runs alongside ordered state changes.
        if (work.ConsumeFrameBudget)
            await WaitForBattleEffectBudgetAsync();
        if (!CanExecuteBattleEffects)
            return;

        BattleEffectScope previous = _battleEffectScope.Value;
        var scope = new BattleEffectScope();
        _battleEffectScope.Value = scope;
        try {
            await work.Execute();
            await DrainBattleEffectScopeAsync(scope);
        }
        finally {
            scope.Active = false;
            scope.Pending.Clear();
            _battleEffectScope.Value = previous;
        }
    }

    private async Task DrainBattleEffectScopeAsync(BattleEffectScope scope) {
        if (scope.Draining)
            return;
        scope.Draining = true;
        try {
            while (CanExecuteBattleEffects && scope.Pending.Count > 0)
                await RunBattleEffectAsync(scope.Pending.Dequeue());
        }
        finally {
            scope.Draining = false;
        }
    }

    private async Task WaitForBattleEffectBudgetAsync() {
        while (CanExecuteBattleEffects) {
            ulong frame = Engine.GetProcessFrames();
            if (_battleEffectFrame != frame) {
                _battleEffectFrame = frame;
                _battleEffectsStartedThisFrame = 0;
            }
            if (_battleEffectsStartedThisFrame < Math.Max(1, EffectsPerFrame)) {
                _battleEffectsStartedThisFrame++;
                return;
            }
            await WaitForBattleEffectFrameAsync();
        }
    }

    internal async Task WaitForBattleEffectFrameAsync() {
        if (!CanExecuteBattleEffects)
            return;
        Task frame = AwaitBattleProcessFrameAsync(GetTree());
        await Task.WhenAny(frame, _battleEffectsStopped.Task);
    }

    private async Task AwaitBattleProcessFrameAsync(SceneTree tree) {
        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private void CancelBattleEffects() {
        _battleEffectsStopped.TrySetResult(true);
        _activeBattleEffect?.Completion.TrySetResult(false);
        while (_battleEffects.Count > 0)
            _battleEffects.Dequeue().Completion.TrySetResult(false);
    }
}
