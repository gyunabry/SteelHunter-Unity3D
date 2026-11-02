using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleResultView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup cg;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button returnButton;

    [Header("연출")]
    [SerializeField, Min(0.1f)] private float fadeDuration = 0.5f;

    private Tween fadeTween;

    public event Action ReturnRequested;

    private void Awake()
    {
        if (cg == null)
        {
            cg = panel.GetComponent<CanvasGroup>();
        }

        Hide();
    }

    private void OnEnable()
    {
        if (returnButton != null)
        {
            returnButton.onClick.AddListener(HandleReturnClicked);
        }
    }

    private void OnDisable()
    {
        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(HandleReturnClicked);
        }
    }

    public void Show(SortieResult result)
    {
        KillFade();

        titleText.text = result switch
        {
            SortieResult.Success => "철수 성공!",
            SortieResult.Destroyed => "죽었다...",
            SortieResult.Victory => "승리!\n보스를 처치했다.",
            SortieResult.Abandoned => "출격 포기...",
            _ => "출격 종료"
        };

        cg.alpha = 0f;

        // 등장하는 동안 버튼 입력 방지
        cg.interactable = false;
        cg.blocksRaycasts = true;
        returnButton.interactable = false;

        panel.SetActive(true);

        fadeTween = cg
            .DOFade(1f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                cg.interactable = true;
                returnButton.interactable = true;
            })
            .OnKill(() => fadeTween = null);
    }

    public void Hide()
    {
        KillFade();

        if (cg != null)
        {
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void HandleReturnClicked()
    {
        cg.interactable = false;
        returnButton.interactable = false;

        ReturnRequested?.Invoke();
    }

    private void KillFade()
    {
        if (fadeTween == null) return;

        fadeTween.Kill();
        fadeTween = null;
    }
}
