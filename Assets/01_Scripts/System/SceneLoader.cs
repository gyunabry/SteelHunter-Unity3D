using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : Singleton<SceneLoader>
{
    private bool loading;

    public void EnterBattleField()
    {
        TryEnterBattleField();
    }

    public bool TryEnterBattleField()
    {
        InventorySessionBinding binding = InventorySessionBinding.Current;

        if (binding == null ||
            !binding.IsReady ||
            loading ||
            binding.SceneType != SceneType.Hangar ||
            InventorySession.Instance.State != GameState.Hangar)
        {
            return false;
        }

        return TryLoad(SceneType.Game, true, binding);
    }

    public void ExitBattleField()
    {
        if (InventorySession.Instance.State == GameState.Battle)
        {
            BattleFlowController flow = FindAnyObjectByType<BattleFlowController>();
            if (flow != null) flow.AbandonSortie();
            else Debug.LogError("출격 포기를 처리할 BattleFlowController가 없습니다.");
            return;
        }

        TryExitBattleField();
    }

    public bool TryExitBattleField()
    {
        InventorySessionBinding binding = InventorySessionBinding.Current;
        if (binding == null || !binding.IsReady || loading ||
            binding.SceneType != SceneType.Game ||
            InventorySession.Instance.State != GameState.Result)
            return false;

        // 성공/실패 결과는 이미 저장됐다. 전장 캐릭터를 다시 캡처하지 않는다.
        return TryLoad(SceneType.Hangar, false, binding);
    }

    private bool TryLoad(SceneType target, bool capturePlayer, InventorySessionBinding binding)
    {
        string sceneName = SceneNames.GetSceneName(target);
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"로드할 씬이 빌드 씬 목록에 없습니다: {sceneName}");
            return false;
        }

        InventorySession session = InventorySession.Instance;
        GameState previousState = session.State;
        float previousTimeScale = Time.timeScale;
        bool previousAudioPaused = GameAudioPause.IsPaused;

        try
        {
            binding.LockControls();
            if (capturePlayer) session.StorePlayer(binding.CaptureSnapshot());

            loading = true;
            session.BeginLoading();
            Time.timeScale = 1f;
            GameAudioPause.SetPaused(false);
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            return true;
        }
        catch (Exception exception)
        {
            loading = false;
            session.CancelLoading(previousState);
            Time.timeScale = previousTimeScale;
            GameAudioPause.SetPaused(previousAudioPaused);
            binding.UnlockControls();
            Debug.LogException(exception, this);
            return false;
        }
    }

    internal void NotifySceneReady(SceneType scene)
    {
        InventorySession.Instance.MarkSceneReady(scene);
        loading = false;
    }
}
