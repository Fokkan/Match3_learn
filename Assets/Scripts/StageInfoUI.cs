using TMPro;
using UnityEngine;

public class StageInfoUI : MonoBehaviour
{
    [Header("DB")]
    [SerializeField] private StageDatabase stageDB;

    [Header("UI")]
    [SerializeField] private TMP_Text stageNumberText;      // 예: "STAGE 1"
    [SerializeField] private TMP_Text stageDescriptionText; // 하단 설명

    public void SetStage(StageData stage)
    {
        if (stage == null) return;

        if (stageNumberText != null)
            stageNumberText.text = $"STAGE {stage.stageID}";

        if (stageDescriptionText != null)
            stageDescriptionText.text = stage.stageDescription ?? "";
    }

    public void SetStageByIndex(int index)
    {
        if (stageDB == null) return;

        // index(0-based)로 stage를 얻되, 내부 표시는 stageID 사용
        StageData stage = stageDB.GetStageByIndex(index);
        SetStage(stage);
    }

}
