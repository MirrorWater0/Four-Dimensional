using System;
using System.Collections.Generic;
using Godot;

/// <summary>One visual seed per attack; connected, crossing rebounds form a multi-hit gesture.</summary>
public sealed class SwordSlashSequence {
    // Painted tips in units of Length, with the source canvas at 1.5 * Length.
    internal static readonly Vector2 NativeStart = new(0.425f, -0.407f);
    private readonly Random _random;
    private readonly List<Strike> _strikes = new();
    private readonly float _openingAngle;
    private readonly float _handedness;
    private readonly float _halfWidth;
    private readonly float _halfHeight;
    private readonly float _upperInset;
    private readonly float _lowerInset;
    private Vector2 _anchor;

    public int Seed { get; }
    public int HitCount { get; }

    internal readonly record struct Strike(Vector2 Offset, float AngleDegrees,
        float LengthScale, float Thickness, float Speed, float Brightness) {
        internal static Strike Identity => new(Vector2.Zero, 0f, 1f, 1f, 1f, 1f);
    }

    public SwordSlashSequence(int hitCount = 1, int seed = 0) {
        HitCount = Math.Max(1, hitCount);
        Seed = seed == 0 ? CreateSeed() : seed;
        // This generator never advances combat, target-selection, or card-draw RNG.
        _random = new Random(Seed);
        _openingAngle = Mathf.DegToRad(Range(-14f, 14f)
            + (_random.Next(2) == 0 ? 0f : 180f));
        _handedness = _random.Next(2) == 0 ? -1f : 1f;
        _halfWidth = Range(0.44f, 0.51f);
        _halfHeight = Range(0.49f, 0.56f);
        _upperInset = Range(0.18f, 0.28f);
        _lowerInset = Range(0.10f, 0.22f);
        _anchor = CreateAnchor(0);
    }

    internal static int CreateSeed() =>
        System.Security.Cryptography.RandomNumberGenerator.GetInt32(1, int.MaxValue);

    internal Strike GetStrike(int index) {
        index = Math.Clamp(index, 0, HitCount - 1);
        while (_strikes.Count <= index) {
            int next = _strikes.Count;
            _strikes.Add(HitCount == 1 ? CreateSingle() : CreateConnected(next));
        }
        return _strikes[index];
    }

    private Strike CreateSingle() {
        float angle = Range(-34f, 34f) + (_random.Next(2) == 0 ? 0f : 180f);
        Vector2 offset = Vector2.FromAngle(Range(-Mathf.Pi, Mathf.Pi)) * Range(0f, 0.035f);
        return new Strike(offset, angle, Range(0.9f, 1.1f), Range(0.86f, 1.14f),
            Range(0.94f, 1.08f), Range(0.95f, 1.06f));
    }

    private Strike CreateConnected(int index) {
        // Lower left -> upper right -> lower right -> upper left.
        // Each hit folds back; the third cut crosses the first instead of orbiting the target.
        bool finisher = HitCount >= 3 && index == HitCount - 1;
        Vector2 end = CreateAnchor(index + 1);
        if (finisher) end = _anchor + (end - _anchor) * 1.08f;
        Vector2 center = (_anchor + end) * 0.5f;
        float angle = Mathf.RadToDeg(Mathf.Wrap((_anchor - end).Angle() - NativeStart.Angle(),
            -Mathf.Pi, Mathf.Pi));
        float lengthScale = _anchor.DistanceTo(end) / (NativeStart.Length() * 2f);
        var strike = new Strike(center, angle, lengthScale,
            Range(0.94f, 1.08f) * (finisher ? 1.16f : 1f),
            Range(0.96f, 1.04f) * (finisher ? 0.94f : 1f),
            Range(0.98f, 1.04f) * (finisher ? 1.08f : 1f));
        // All anchors stay around the target; long combos cannot drift off the character.
        _anchor = end;
        return strike;
    }

    private Vector2 CreateAnchor(int index) {
        Vector2 point = (index % 4) switch {
            0 => new Vector2(-_halfWidth, _halfHeight * 0.65f),
            1 => new Vector2(_halfWidth, -_halfHeight),
            2 => new Vector2(_lowerInset, _halfHeight),
            _ => new Vector2(-_upperInset, -_halfHeight)
        };
        point += new Vector2(Range(-0.025f, 0.025f), Range(-0.025f, 0.025f));
        point.X *= _handedness;
        return point.Rotated(_openingAngle);
    }

    private float Range(float min, float max) => min + (max - min) * (float)_random.NextDouble();
}
