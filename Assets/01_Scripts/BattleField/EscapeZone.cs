using System;
using System.Collections.Generic;
using UnityEngine;

/*
 플레이어가 이 존 안에 5초 동안 있으면 탈출
 */

public class EscapeZone : MonoBehaviour
{
    [Header("탈출 대상")]
    [SerializeField] private CharacterRoot playerRoot;

    [Header("탈출 조건")]
    [SerializeField, Min(0.1f)] private float requiredStayTime = 5f;

    private readonly HashSet<Collider> playerColliders = new();

    private float elapsedTime;
    private bool completed;

    public event Action EscapeReady;

    public float Progress => Mathf.Clamp01(elapsedTime / requiredStayTime);
    public float RemainingTime => Mathf.Max(0f, requiredStayTime - elapsedTime);

    public bool IsEscaping => !completed && playerColliders.Count > 0;

    private void Awake()
    {
        if (playerRoot == null)
        {
            Debug.LogError("Player가 연결되어 있지 않습니다");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (completed) return;

        CharacterRoot player = other.GetComponentInParent<CharacterRoot>();

        if (playerRoot != player) return;

        playerColliders.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!playerColliders.Remove(other)) return;

        if (playerColliders.Count == 0)
        {
            elapsedTime = 0;
        }
    }

    private void Update()
    {
        if (completed) return;

        if (playerColliders.Count <= 0) return;

        elapsedTime += Time.deltaTime;

        if (elapsedTime < requiredStayTime) return;

        elapsedTime = requiredStayTime;
        completed = true;

        EscapeReady?.Invoke();
    }
}
