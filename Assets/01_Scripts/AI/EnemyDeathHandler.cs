using UnityEngine;

public class EnemyDeathHandler : MonoBehaviour
{
    [SerializeField, Min(0f)] private float despawnDelay = 2f;

    private CharacterHealth health;

    private void Awake()
    {
        health = GetComponent<CharacterHealth>();
    }

    private void OnEnable()
    {
        health.Died += HandleDied;
    }

    private void OnDisable()
    {
        health.Died -= HandleDied;
    }

    private void HandleDied()
    {
        Destroy(gameObject, despawnDelay);
    }
}
