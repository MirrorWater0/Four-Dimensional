using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Skill {
    private Task AttackAreaAsync(Character[] targets, int damage, int times,
        bool prepareEveryHit = false) => OwnerCharater?.BattleNode is Battle battle
        ? battle.ExecuteBattleEffectAsync(() => ExecuteAreaAttackAsync(targets, damage, times,
            prepareEveryHit), "范围攻击", consumeFrameBudget: false)
        : ExecuteAreaAttackAsync(targets, damage, times, prepareEveryHit);

    private bool CanResolveAreaAttack() => OwnerCharater != null
        && GodotObject.IsInstanceValid(OwnerCharater) && !ShouldAbortStepExecution(this);

    private bool IsLiveAreaAttackTarget(Character target) => target != null
        && GodotObject.IsInstanceValid(target) && target.State == Character.CharacterState.Normal
        && !IsDummyTarget(this, target);

    private async Task ExecuteAreaAttackAsync(Character[] targets, int damage, int times,
        bool prepareEveryHit) {
        if (!CanResolveAreaAttack() || targets == null || times <= 0)
            return;
        Character[] targetSnapshot = targets.Where(IsLiveAreaAttackTarget).ToArray();
        if (targetSnapshot.Length == 0)
            return;

        // One windup for the entire attack. Impact visuals never wait for another
        // target's damage, death callbacks, or frame budget to finish.
        CharacterEffect.Spawn(OwnerCharater, "explode");
        await Task.Delay(300);
        if (!CanResolveAreaAttack())
            return;
        var prepared = new bool[targetSnapshot.Length];
        var targetDamage = new int[targetSnapshot.Length];
        Battle battle = OwnerCharater?.BattleNode;
        var slashSequence = new SwordSlashSequence(times);
        battle?.BeginDeferredDyingResolution();
        try {
            for (int hit = 0; hit < times; hit++) {
                if (!CanResolveAreaAttack())
                    return;
                Character[] liveTargets = targetSnapshot.Where(IsLiveAreaAttackTarget).Distinct().ToArray();
                if (liveTargets.Length == 0)
                    return;
                AudioManager.PlayAttack(OwnerCharater);
                foreach (Character target in liveTargets)
                    SpawnAttackHitEffect(target, slashSequence, hit);

                // Flush effects queued before this wave once. Effects caused by one
                // target in the wave must not delay damage to its siblings.
                if (battle != null)
                    await battle.ResolvePendingBattleEffectsAsync();

                for (int index = 0; index < targetSnapshot.Length; index++) {
                    if (!CanResolveAreaAttack())
                        return;
                    int targetIndex = index;
                    Character target = targetSnapshot[index];
                    if (!IsLiveAreaAttackTarget(target))
                        continue;

                    if (prepareEveryHit || !prepared[targetIndex]) {
                        // The legacy AOE entry point prepares each separate hit;
                        // skill-plan multi-hit attacks consume modifiers once per target.
                        if (!CanResolveAreaAttack() || !IsLiveAreaAttackTarget(target))
                            continue;
                        targetDamage[targetIndex] = Math.Clamp(AttackBuff.ApplyOutgoingDamageModifiers(
                            OwnerCharater, Math.Clamp(damage, 0, 9999), target, consumeStacks: true), 0, 9999);
                        prepared[targetIndex] = true;
                    }
                    await target.GetHurt(targetDamage[targetIndex], OwnerCharater,
                        damageKind: Character.DamageKind.Attack);
                }

                // Multi-hit attacks advance as whole waves, rather than replaying all
                // hits on one target before starting the animation on the next target.
                if (hit < times - 1)
                    await Task.Delay(100);
            }
        }
        finally {
            if (battle != null)
                await battle.EndDeferredDyingResolutionAsync();
        }
    }
}
