using System;
using UnityEngine;

[RequireComponent(typeof(CharacterRoot), typeof(CharacterMover))]
[DefaultExecutionOrder(300)]
public sealed class CharacterNoiseEmitter : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float stepDistance = 1.5f;
    [SerializeField, Min(0f)] private float walkRadius = 5f;
    [SerializeField, Min(0f)] private float sprintRadius = 10f;

    private CharacterRoot owner;
    private CharacterMover mover;
    private float distanceSinceStep;

    public event Action<Vector3, bool> Stepped;

    private void Awake()
    {
        owner = GetComponent<CharacterRoot>();
        mover = GetComponent<CharacterMover>();
    }

    private void OnEnable()
    {
        distanceSinceStep = 0f;
    }

    private void OnDisable()
    {
        distanceSinceStep = 0f;
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f) return;

        if (owner.Health == null ||
            !owner.Health.IsAlive ||
            !mover.isActiveAndEnabled ||
            !mover.IsInitialized ||
            !mover.IsGrounded)
        {
            distanceSinceStep = 0f;
            return;
        }

        float speed = mover.PlaneVelocity.magnitude;

        if (speed < 0.05f)
        {
            distanceSinceStep = 0f;
            return;
        }

        // 실제 수평 이동 거리만 누적
        distanceSinceStep += speed * Time.deltaTime;

        float requiredDistance = Mathf.Max(0.1f, stepDistance);
        if (distanceSinceStep < requiredDistance)
            return;

        distanceSinceStep %= requiredDistance;

        // 기존 발소리 반경 선택 기준을 유지
        bool isSprinting = mover.IsSprinting;
        float radius = isSprinting ? sprintRadius : walkRadius;

        CharacterStimulusBus.ReportSound(
            owner,
            transform.position,
            CharacterSoundKind.Footstep,
            radius);

        // Audible feedback is independent of the AI hearing radius.
        Stepped?.Invoke(transform.position, isSprinting);
    }
}