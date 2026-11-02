using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EscapeProgressPresenter : MonoBehaviour
{
    [Header("탈출 구역")]
    [SerializeField] private EscapeZone[] escapeZones;

    [Header("뷰")]
    [SerializeField] private TimeProgressView view;

    private void Awake()
    {
        if (view == null)
        {
            Debug.LogError("EscapeProgressView의 View를 연결하세요.", this);
        }
    }


    private void LateUpdate()
    {
        // 결과 화면이나 일시정지 상태에서는 탈출 UI를 숨긴다.
        if (InventorySession.Instance.State != GameState.Battle)
        {
            view.Hide();
            return;
        }

        EscapeZone activeZone = FindActiveZone();

        if (activeZone == null)
        {
            view.Hide();
            return;
        }

        view.Show("철수 중", activeZone.Progress, activeZone.RemainingTime);
    }


    private EscapeZone FindActiveZone()
    {
        if (escapeZones == null) return null;

        foreach (EscapeZone zone in escapeZones)
        {
            if (zone == null || !zone.isActiveAndEnabled)
            {
                continue;
            }

            if (zone.IsEscaping) return zone;
        }

        return null;
    }

    private void OnDisable()
    {
        if (view != null) view.Hide();
    }
}
