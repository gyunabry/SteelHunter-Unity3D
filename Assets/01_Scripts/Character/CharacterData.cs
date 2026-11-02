using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "SteelHunter/Character/Character Data")]
public sealed class CharacterData : ScriptableObject
{
    [SerializeField, Min(0.01f)] private float walkSpeed = 5f;
    [SerializeField, Min(0.01f)] private float sprintSpeed = 7.5f;
    [SerializeField, Min(0.01f)] private float acceleration = 25f;
    [SerializeField, Min(0.01f)] private float turnSpeed = 720f;
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    public float WalkSpeed => walkSpeed;
    public float SprintSpeed => Mathf.Max(walkSpeed, sprintSpeed);
    public float Acceleration => acceleration;
    public float TurnSpeed => turnSpeed;
    public float MaxHealth => maxHealth;
}
