using System;
using UnityEngine;

public class BattleFlowController : MonoBehaviour
{
    [Header("탈출 구역")]
    [SerializeField] private EscapeZone[] escapeZones;

    [Header("플레이어")]
    [SerializeField] private PlayerCharacterInput playerInput;
    [SerializeField] private CharacterHealth playerHealth;
    [SerializeField] private InventorySessionBinding sessionBinding;

    [Header("결과 화면")]
    [SerializeField] private BattleResultView resultView;

    [Header("보스 스포너")]
    [SerializeField] private EnemySpawnManager bossSpawner;

    private CharacterHealth bossHealth;

    public GameState State => InventorySession.Instance.State;

    private void Awake()
    {
        if (sessionBinding == null && playerInput != null)
        {
            sessionBinding = playerInput.GetComponent<InventorySessionBinding>();
        }
            
        if (resultView != null) resultView.Hide();
    }

    private void OnEnable()
    {
        if (escapeZones != null)
        {
            foreach (EscapeZone zone in escapeZones)
                if (zone != null) zone.EscapeReady += HandleEscapeReady;
        }
        if (playerHealth != null) playerHealth.Died += HandlePlayerDied;
        if (resultView != null) resultView.ReturnRequested += HandleReturnRequested;
        if (bossSpawner != null) bossSpawner.EnemySpawned += RegisterBoss;

        RegisterBoss(bossSpawner.LastSpawnedHealth);
    }

    private void OnDisable()
    {
        if (escapeZones != null)
        {
            foreach (EscapeZone zone in escapeZones)
                if (zone != null) zone.EscapeReady -= HandleEscapeReady;
        }
        if (playerHealth != null) playerHealth.Died -= HandlePlayerDied;
        if (resultView != null) resultView.ReturnRequested -= HandleReturnRequested;
        if (bossSpawner != null) bossSpawner.EnemySpawned -= RegisterBoss;

        UnregisterBoss();
    }

    private void HandleEscapeReady()
    {
        if (playerHealth == null || !playerHealth.IsAlive) return;
        ShowResult(SortieResult.Success);
    }

    private void HandlePlayerDied()
    {
        ShowResult(SortieResult.Destroyed);
    }

    public void AbandonSortie()
    {
        ShowResult(SortieResult.Abandoned);
    }

    private void ShowResult(SortieResult result)
    {
        if (State != GameState.Battle || sessionBinding == null ||
            !sessionBinding.IsReady || resultView == null)
            return;

        sessionBinding.LockControls();
        try
        {
            if (!InventorySession.Instance.TryResolveSortie(result, sessionBinding))
            {
                sessionBinding.UnlockControls();
                return;
            }

            Time.timeScale = 0f;
            GameAudioPause.SetPaused(true);
            GameCursor.Current.ShowPointer();
            resultView.Show(result);
        }
        catch (Exception exception)
        {
            sessionBinding.UnlockControls();
            Debug.LogException(exception, this);
        }
    }

    private void HandleReturnRequested()
    {
        if (State != GameState.Result) return;

        if (!SceneLoader.Instance.TryExitBattleField())
        {
            // 기존 BattleResultView는 클릭하면 버튼을 비활성화한다.
            // 이동 실패 시 다시 표시해 귀환 버튼을 사용할 수 있게 한다.
            SortieResult? result = InventorySession.Instance.LastResult;
            if (result.HasValue && resultView != null)
            {
                resultView.Show(result.Value);
            }
        }
    }

    private void RegisterBoss(CharacterHealth health)
    {
        UnregisterBoss();

        if (health == null)
        {
            return;
        }

        bossHealth = health;

        // 보스가 사망하면 승리 처리를 실행
        bossHealth.Died += HandleBossDefeated;

        // 연결 시점에 이미 죽어 있는 경우도 처리
        if (!bossHealth.IsAlive)
        {
            HandleBossDefeated();
        }
    }

    private void UnregisterBoss()
    {
        if (bossHealth != null)
        {
            bossHealth.Died -= HandleBossDefeated;
        }

        bossHealth = null;
    }

    private void HandleBossDefeated()
    {
        if (playerHealth == null || !playerHealth.IsAlive)
        {
            return;
        }

        ShowResult(SortieResult.Victory);
    }
}
