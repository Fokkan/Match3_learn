using UnityEngine;

public class GamePlayStageBootstrap : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private StageManager stageManager;

    [Header("PlayerPrefs Key")]
    [SerializeField] private string selectedStageKey = "SelectedStageId";

    [Header("Fallback")]
    [SerializeField] private int defaultStageId = 0;
    private void Awake()
    {
        // 씬 로드 시 timeScale 꼬임 방지 (StageSelect/옵션창에서 넘어와도 항상 정상화)
        Time.timeScale = 1f;

#if DOTWEEN
        DG.Tweening.DOTween.KillAll(true);
#endif

        // 기존 Awake 로직이 있으면 그 아래 그대로 유지
    }

    private void Start()
    {
        if (stageManager == null)
        {
            Debug.LogError("[GamePlayStageBootstrap] StageManager reference is missing.");
            return;
        }

        int stageId = PlayerPrefs.GetInt(selectedStageKey, defaultStageId);
        if (stageId <= 0) stageId = 1;
        Debug.Log($"[GamePlayStageBootstrap] Auto load stageId={stageId}");
        stageManager.SelectStage(stageId);


        stageManager.SelectStage(stageId);
    }
}
