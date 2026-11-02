using System;
using UnityEngine;

public enum CharacterSoundKind
{
    Footstep,
    Gunshot
}

public readonly struct CharacterSound
{
    public readonly CharacterRoot Source;
    public readonly Vector3 Position;
    public readonly CharacterSoundKind Kind;
    public readonly float Radius;

    public CharacterSound(CharacterRoot source, Vector3 position, CharacterSoundKind kind, float radius)
    {
        Source = source;
        Position = position;
        Kind = kind;
        Radius = radius;
    }
}

public static class CharacterStimulusBus
{
    public static event Action<CharacterSound> SoundReported;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        SoundReported = null;
    }

    public static void ReportSound(CharacterRoot source, Vector3 position, CharacterSoundKind kind, float radius)
    {
        if (source == null || source.Health == null || !source.Health.IsAlive || radius <= 0f) return;
        SoundReported?.Invoke(new CharacterSound(source, position, kind, radius));
    }
}