using UnityEngine;

[CreateAssetMenu(fileName = "WeaponAudioProfile", menuName = "SteelHunter/Audio/Weapon Audio Profile")]
public sealed class WeaponAudioProfile : ScriptableObject
{
    [SerializeField] private SoundCue fire;
    [SerializeField] private SoundCue reloadStart;
    [SerializeField] private SoundCue reloadComplete;

    public SoundCue Fire => fire;
    public SoundCue ReloadStart => reloadStart;
    public SoundCue ReloadComplete => reloadComplete;
}
