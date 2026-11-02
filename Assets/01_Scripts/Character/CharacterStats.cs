using UnityEngine;

/// <summary>Per-character runtime stats, refreshed when equipment changes.</summary>
[DisallowMultipleComponent]
public sealed class CharacterStats : MonoBehaviour
{
    private CharacterEquipment equipment;
    private EnemyCharacterBrain enemyBrain;

    public int ArmorLevel { get; private set; } = -1;

    private void OnEnable()
    {
        equipment = GetComponent<CharacterEquipment>();
        enemyBrain = GetComponent<EnemyCharacterBrain>();
        if (equipment != null) equipment.Changed += RefreshArmorLevel;
        RefreshArmorLevel();
    }

    private void OnDisable()
    {
        if (equipment != null) equipment.Changed -= RefreshArmorLevel;
    }

    private void RefreshArmorLevel()
    {
        ArmorData head;
        ArmorData body;
        if (equipment != null)
        {
            head = equipment.HeadArmor;
            body = equipment.BodyArmor;
        }
        else if (enemyBrain != null)
        {
            EnemyCharacterData profile = enemyBrain.Profile;
            head = profile != null ? profile.HeadArmor : null;
            body = profile != null ? profile.BodyArmor : null;
        }
        else
        {
            head = null;
            body = null;
        }
        if (head != null && head.Slot != ArmorSlot.Head) head = null;
        if (body != null && body.Slot != ArmorSlot.Body) body = null;

        ArmorLevel = head == null && body == null
            ? -1
            : (head != null ? head.ArmorLevel : 0) +
              (body != null ? body.ArmorLevel : 0);
    }
}
