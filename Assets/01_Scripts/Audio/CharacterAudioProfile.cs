using UnityEngine;

[CreateAssetMenu(fileName = "CharacterAudioProfile", menuName = "SteelHunter/Audio/Character Audio Profile")]
public sealed class CharacterAudioProfile : ScriptableObject
{
    [SerializeField] private SoundCue walkFootstep;
    [SerializeField] private SoundCue sprintFootstep;
    [SerializeField] private SoundCue hurt;
    [SerializeField] private SoundCue death;

    public SoundCue WalkFootstep => walkFootstep;
    public SoundCue SprintFootstep => sprintFootstep != null ? sprintFootstep : walkFootstep;
    public SoundCue Hurt => hurt;
    public SoundCue Death => death;
}
