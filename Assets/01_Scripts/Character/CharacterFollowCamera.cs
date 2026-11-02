using UnityEngine;

/// <summary>Fixed-angle follow camera for the isolated character combat lab.</summary>
public sealed class CharacterFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 18f, -12f);
    [SerializeField, Min(0.01f)] private float followSpeed = 12f;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        float blend = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, blend);
        transform.rotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
    }
}
