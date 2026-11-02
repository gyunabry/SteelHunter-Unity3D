using UnityEngine;

public static class GameAudioPause
{
    public static bool IsPaused => AudioListener.pause;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => AudioListener.pause = false;

    public static void SetPaused(bool paused) => AudioListener.pause = paused;
}
