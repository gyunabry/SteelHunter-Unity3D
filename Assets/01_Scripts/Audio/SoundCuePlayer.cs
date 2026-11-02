using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class SoundCuePlayer : MonoBehaviour
{
    [SerializeField] private bool playWhilePaused;

    private AudioSource audioSource;

    private void Awake()
    {
        EnsureSource();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 1f;
        audioSource.pitch = 1f;
        audioSource.dopplerLevel = 0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.Stop();
    }

    private void OnEnable()
    {
        EnsureSource();

        audioSource.ignoreListenerPause = playWhilePaused;
    }

    private void EnsureSource()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public bool Play(SoundCue cue, bool interrupt = false)
    {
        if (cue == null || cue.Clip == null || !isActiveAndEnabled) return false;
        EnsureSource();

        if (!audioSource.isActiveAndEnabled ||
            (AudioListener.pause && !audioSource.ignoreListenerPause)) return false;

        if (interrupt) audioSource.Stop();

        audioSource.minDistance = cue.MinDistance;
        audioSource.maxDistance = cue.MaxDistance;
        audioSource.PlayOneShot(cue.Clip, cue.Volume);

        return true;
    }

    public void Stop()
    {
        if (audioSource != null) audioSource.Stop();
    }

    private void OnDisable() => Stop();
}
