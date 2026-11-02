using UnityEngine;

[CreateAssetMenu(fileName = "EnemyCharacterData", menuName = "SteelHunter/AI/Enemy Character Data")]
public sealed class EnemyCharacterData : ScriptableObject
{
    [Header("°¨°¢")]
    [Min(1f)] public float detectionRange = 22f;
    [Min(1f)] public float loseRange = 28f;
    [Range(1f, 360f)] public float fieldOfView = 110f;
    [Min(0.01f)] public float confirmationTime = 0.35f;
    [Min(0.01f)] public float perceptionInterval = 0.15f;
    [Min(0f)] public float soundOcclusionFactor = 0.5f;
    [Min(0f)] public float investigationDuration = 5f;
    [Min(0f)] public float sightMemoryDuration = 6f;
    [Min(0f)] public float searchDuration = 5f;
    [Min(0f)] public float pursuitLimit = 12f;

    [Header("ÀüÅõ")]
    [Min(0.01f)] public float thinkInterval = 0.2f;
    public Vector2 reactionDelay = new Vector2(0.25f, 0.6f);
    [Min(0f)] public float preferredMinRange = 8f;
    [Min(0f)] public float preferredMaxRange = 16f;
    [Min(0f)] public float rangeHysteresis = 1.5f;
    [Range(0f, 90f)] public float aimTolerance = 5f;
    public Vector2Int burstRounds = new Vector2Int(2, 4);
    public Vector2 burstPause = new Vector2(0.5f, 1f);
    [Range(0f, 1f)] public float retreatHealthFraction = 0.35f;
    [Min(0f)] public float pursuitRadius = 30f;

    [Header("°íÁ¤ ÃÑ±â/Åº¾à")]
    [SerializeField] private FirearmData firearm;
    [SerializeField] private AmmoData ammo;

    [Header("Armor")]
    [SerializeField] private ArmorData headArmor;
    [SerializeField] private ArmorData bodyArmor;

    [Header("ÀÌµ¿ ¹× ¾öÆó")]
    [Min(0f)] public float waypointWait = 1f;
    [Min(0.1f)] public float coverSearchRadius = 10f;
    [Min(0.1f)] public float reloadCoverTimeout = 2f;
    [Min(0.1f)] public float pathRetryInterval = 0.5f;
    [Min(0.1f)] public float stuckTimeout = 2f;

    public FirearmData Firearm => firearm;
    public AmmoData Ammo => ammo;
    public ArmorData HeadArmor => headArmor;
    public ArmorData BodyArmor => bodyArmor;

    private void OnValidate()
    {
        if (headArmor != null && headArmor.Slot != ArmorSlot.Head)
            Debug.LogError("Head armor must use the Head slot.", this);
        if (bodyArmor != null && bodyArmor.Slot != ArmorSlot.Body)
            Debug.LogError("Body armor must use the Body slot.", this);

        loseRange = Mathf.Max(detectionRange, loseRange);
        preferredMaxRange = Mathf.Max(preferredMinRange, preferredMaxRange);
        burstRounds.y = Mathf.Max(burstRounds.x, burstRounds.y);
        reactionDelay.y = Mathf.Max(reactionDelay.x, reactionDelay.y);
        burstPause.y = Mathf.Max(burstPause.x, burstPause.y);
    }
}
