using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private Transform leftHandGrip;
    [SerializeField] private Transform muzzlePoint;
    
    public Transform LeftHandGrip => leftHandGrip;
    public Transform MuzzlePoint => muzzlePoint;
}
