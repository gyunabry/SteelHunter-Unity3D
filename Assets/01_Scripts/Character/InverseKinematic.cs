using UnityEngine;

[RequireComponent(typeof(Animator))]
public class InverseKinematic : MonoBehaviour
{
    private Animator animator;
    private Transform leftHand;
    private int activeLayerIndex = -1;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void SetWeaponIK(Transform target, int layerIndex)
    {
        leftHand = target;
        activeLayerIndex = layerIndex;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (layerIndex != activeLayerIndex)
        {
            return;
        }

        bool hasTarget = leftHand != null;
        float weight = hasTarget ? 1f : 0f;

        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);

        if (!hasTarget) return;

        animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHand.position);
        animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHand.rotation);
    }
}
