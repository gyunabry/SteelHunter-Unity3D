using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class CharacterMover : MonoBehaviour
{
    [SerializeField] private float gravity = -9.81f;
    [SerializeField, Min(0f)] private float groundStickSpeed = 2f;

    private CharacterController controller;
    private CharacterData data;
    private CharacterGun gun;

    private Vector3 moveDirection;
    private bool sprintRequested;

    private Vector3 horizontalVelocity;
    private float verticalSpeed;

    public bool IsInitialized => controller != null && data != null;

    public Vector3 Velocity { get; private set; }

    public Vector3 PlaneVelocity => new Vector3(Velocity.x, 0, Velocity.z);

    public bool IsGrounded => controller != null && controller.isGrounded;

    public bool IsSprinting => IsInitialized &&
        sprintRequested && (gun == null || !gun.IsAiming) &&
        data.SprintSpeed > data.WalkSpeed &&
        PlaneVelocity.magnitude > Mathf.Lerp(data.WalkSpeed, data.SprintSpeed, 0.5f) *
            (gun != null ? gun.MovementSpeedCoefficient : 1f);

    public void Initialize(CharacterData characterData)
    {
        controller = GetComponent<CharacterController>();
        data = characterData;
        gun = GetComponent<CharacterGun>();

        StopImmediately();
        verticalSpeed = 0f;
    }

    /// <summary>
    /// 플레이어/AI가 호출해 움직임을 제어하는 메서드
    /// </summary>
    /// <param name="worldDirection"></param>
    /// <param name="sprint"></param>
    public void SetMove(Vector3 worldDirection, bool sprint)
    {
        worldDirection.y = 0;

        moveDirection = Vector3.ClampMagnitude(worldDirection, 1f);
        sprintRequested = sprint;
    }

    private void Update()
    {
        if (!IsInitialized || !controller.enabled)
        {
            Velocity = Vector3.zero;
            return;
        }

        float speed = sprintRequested && (gun == null || !gun.IsAiming)
            ? data.SprintSpeed
            : data.WalkSpeed;
        speed *= gun != null ? gun.MovementSpeedCoefficient : 1f;

        Vector3 targetVelocity = moveDirection * speed;

        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, data.Acceleration * Time.deltaTime);

        // 바닥이라면 작은 하향 속도로 바닥면 접촉을 유지
        // 공중에서는 중력을 적용
        if (controller.isGrounded && verticalSpeed <= 0f)
        {
            verticalSpeed = -groundStickSpeed;
        }
        else
        {
            verticalSpeed += gravity * Time.deltaTime;
        }

        // 수평 속도와 수직 속도를 합한다
        Vector3 velocity = horizontalVelocity + Vector3.up * verticalSpeed;

        // CharacterController에는 이동 거리만 전달
        controller.Move(velocity * Time.deltaTime);

        Velocity = controller.velocity;
    }

    public void StopImmediately()
    {
        moveDirection = Vector3.zero;
        sprintRequested = false;
        horizontalVelocity = Vector3.zero;
        Velocity = Vector3.zero;
    }

    private void OnDisable()
    {
        StopImmediately();

        verticalSpeed = 0f;
    }
}
