using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SoundCue", menuName = "SteelHunter/Audio/Sound Cue")]
public sealed class SoundCue : ScriptableObject
{
    [FormerlySerializedAs("clips")]
    [SerializeField] private AudioClip clip;

    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField, Min(0.01f)] private float minDistance = 2f;
    [SerializeField, Min(0.01f)] private float maxDistance = 20f;

    public AudioClip Clip => clip;
    public float Volume => Mathf.Clamp01(volume);

    public float MinDistance => Mathf.Max(0.01f, minDistance);
    public float MaxDistance => Mathf.Max(MinDistance + 0.01f, maxDistance);
}
