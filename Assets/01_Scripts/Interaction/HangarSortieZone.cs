using System;
using System.Collections.Generic;
using UnityEngine;

public class HangarSortieZone : MonoBehaviour
{
    [Header("검사 대상")]
    [SerializeField] private CharacterRoot playerRoot;

    [Header("출격 대기")]
    [SerializeField, Min(0.1f)] private float requiredStayTime = 3f;

    private readonly HashSet<Collider> playerColliders = new();

    private float elapsedTime;

    public event Action SortieReady;

    public CharacterRoot PlayerRoot => playerRoot;

    public bool IsPreparing =>
    isActiveAndEnabled &&
    playerRoot != null &&
    playerColliders.Count > 0;

    public float Progress => Mathf.Clamp01(elapsedTime / requiredStayTime);

    public float RemainingTime => Mathf.Max(0f, requiredStayTime - elapsedTime);

    private void OnTriggerEnter(Collider other)
    {
        TrackPlayerCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TrackPlayerCollider(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!playerColliders.Remove(other)) return;

        if (playerColliders.Count == 0) elapsedTime = 0f;
    }

    private void TrackPlayerCollider(Collider other)
    {
        if (playerRoot == null)
        {
            Debug.LogError("Player의 CharacterRoot가 연결되지 않았습니다.", this);
            return;
        }

        CharacterRoot root = other.GetComponentInParent<CharacterRoot>();

        if (root != playerRoot) return;

        playerColliders.Add(other);
    }

    private void Update()
    {
        if (playerColliders.Count == 0) return;

        elapsedTime = Mathf.Min(requiredStayTime, elapsedTime + Time.deltaTime);

        if (elapsedTime < requiredStayTime) return;

        SortieReady?.Invoke();
    }

    private void OnDisable()
    {
        playerColliders.Clear();

        elapsedTime = 0f;
    }
}
