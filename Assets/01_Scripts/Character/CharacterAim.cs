using UnityEngine;

public sealed class CharacterAim : MonoBehaviour
{
    private CharacterData data;
    private Vector3 targetPoint;
    private bool hasTarget;

    private void Update()
    {
        if (data == null || !hasTarget)
        {
            return;
        }

        Vector3 direction = targetPoint - transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        float desireAim = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        float nextAim = Mathf.MoveTowardsAngle(transform.eulerAngles.y, desireAim, data.TurnSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Euler(0f, nextAim, 0f);
    }

    public void Initialize(CharacterData characterData)
    {
        data = characterData;
        hasTarget = false;
    }

    public void SetAim(Vector3 worldPoint)
    {
        if (data == null)
            return;

        Vector3 direction = worldPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        targetPoint = worldPoint;
        hasTarget = true;
    }

    public void ClearAim()
    {
        hasTarget = false;
    }

    public float HorizontalAimError(Vector3 worldPoint)
    {
        Vector3 direction = worldPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f) return 180f;

        return Vector3.Angle(transform.forward, direction);
    }

    public bool TryGetFireDirection(Vector3 muzzlePosition, out Vector3 direction)
    {
        direction = Vector3.zero;
        if (!hasTarget) return false;

        direction = targetPoint - muzzlePosition;

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = transform.forward;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        direction.Normalize();
        return true;
    }
}
