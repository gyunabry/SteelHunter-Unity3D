using UnityEngine;

public enum CharacterTeam
{
    Player,
    Enemy,
    Neutral
}

[RequireComponent(typeof(CharacterMover))]
[RequireComponent(typeof(CharacterAim))]
[RequireComponent(typeof(CharacterGun))]
[RequireComponent(typeof(CharacterHealth))]
[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(CharacterCommandReceiver))]
public sealed class CharacterRoot : MonoBehaviour
{
    [SerializeField] private CharacterData characterData;
    [SerializeField] private CharacterTeam team = CharacterTeam.Player;

    private CharacterCommandReceiver commands;

    public CharacterTeam Team => team;
    public CharacterHealth Health { get; private set; }
    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        if (characterData == null)
        {
            Debug.LogError("CharacterData가 필요합니다.", this);
            enabled = false;
            return;
        }

        CharacterMover mover = GetComponent<CharacterMover>();
        CharacterAim aim = GetComponent<CharacterAim>();
        CharacterGun gun = GetComponent<CharacterGun>();
        commands = GetComponent<CharacterCommandReceiver>();
        Health = GetComponent<CharacterHealth>();

        mover.Initialize(characterData);
        aim.Initialize(characterData);
        Health.Initialize(characterData.MaxHealth);
        gun.Initialize(this);
        commands.Initialize(mover, aim, gun, Health);
        Health.Died += commands.Stop;
        IsInitialized = mover.IsInitialized && gun.IsInitialized && commands.IsInitialized;
    }

    private void OnDestroy()
    {
        if (Health != null && commands != null) Health.Died -= commands.Stop;
    }

    /// <summary>
    /// 대상과 같은 팀인지 검사
    /// 적끼리 공격 방지
    /// </summary>
    public bool IsFriendlyTo(CharacterRoot other)
    {
        return other != null && team == other.team;
    }
}
