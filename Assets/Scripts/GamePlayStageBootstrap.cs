using UnityEngine;

public class GamePlayStageBootstrap : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private StageManager stageManager;

    [Header("PlayerPrefs Key")]
    [SerializeField] private string selectedStageKey = "SelectedStageId";

    [Header("Fallback")]
    [SerializeField] private int defaultStageId = 0;

    private void Start()
    {
        if (stageManager == null)
        {
            Debug.LogError("[GamePlayStageBootstrap] StageManager reference is missing.");
            return;
        }

        int stageId = PlayerPrefs.GetInt(selectedStageKey, defaultStageId);
        Debug.Log($"[GamePlayStageBootstrap] Auto load stageId={stageId}");

        stageManager.SelectStage(stageId);
    }
}
