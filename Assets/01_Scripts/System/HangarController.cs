using UnityEngine;

public class HangarController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private InventorySessionBinding binding;

    [Header("Sortie Area")]
    [SerializeField] private HangarSortieZone sortieZone;

    [Header("UI")]
    [SerializeField] private TimeProgressView view;

    private bool transitionRequested;

    private void Awake()
    {
        if (binding == null || sortieZone == null || view == null)
        {
            Debug.LogError("HangarSortieController의 참조를 모두 연결하세요.", this);
            return;
        }

        CharacterRoot root = binding.GetComponent<CharacterRoot>();

        if (root == null ||
            sortieZone.PlayerRoot != root)
        {
            Debug.LogError("출격 구역과 Binding은 같은 플레이어를 가리켜야 합니다.", this);
            return;
        }
    }

    private void OnEnable()
    {
        if (sortieZone != null) sortieZone.SortieReady += HandleSortieReady;
    }

    private void LateUpdate()
    {
        if (transitionRequested || sortieZone == null || !sortieZone.IsPreparing)
        {
            view.Hide();
            return;
        }
        view.Show("출격 준비 중", sortieZone.Progress, sortieZone.RemainingTime);
    }

    private void HandleSortieReady()
    {
        if (transitionRequested) return;

        transitionRequested = true;

        if (SceneLoader.Instance.TryEnterBattleField()) return;

        view.Hide();

        Debug.LogWarning(
            "출격에 실패했습니다. 원인을 확인한 뒤 " +
            "출격 구역에서 나갔다가 다시 진입하세요.",
            this);
    }

    private void OnDisable()
    {
        if (sortieZone != null)
        {
            sortieZone.SortieReady -= HandleSortieReady;
        }

        if (view != null) view.Hide();
    }
}
