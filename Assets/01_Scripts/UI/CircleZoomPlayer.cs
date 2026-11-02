using System.Collections;
using UnityEngine;

public class CircleZoomPlayer : MonoBehaviour
{
    private static readonly int StartState = Animator.StringToHash("Base Layer.CircleZoom_Start");
    private static readonly int EndState = Animator.StringToHash("Base Layer.CircleZoom_End");

    [SerializeField] private Animator animator;
    [SerializeField] private CanvasGroup cg;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        cg = GetComponent<CanvasGroup>();

        // timeScale 값이 0이어도 UI 전환은 재생되도록 설정
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;

    }

    // 격납고 -> 전장 전환 시 시작
    public IEnumerator Cover()
    {
        SetOverlayVisible(true);
        yield return PlayAndWait(StartState);
    }

    public IEnumerator Reveal()
    {
        yield return PlayAndWait(EndState);
        SetOverlayVisible(false);
    }

    private IEnumerator PlayAndWait(int stateHash)
    {
        animator.Play(stateHash);

        // Play 직후 같은 프레임에 상태 정보가 이전 상태로 남는 것을 방지
        animator.Update(0f);

        while (true)
        {
            
        }
    }

    private void SetOverlayVisible(bool visible)
    {
        cg.alpha = visible ? 1f : 0f;
        cg.blocksRaycasts = visible;
        cg.interactable = false;
    }
}
