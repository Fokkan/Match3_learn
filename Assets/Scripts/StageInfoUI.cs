using System.Text;
using TMPro;
using UnityEngine;

public class StageInfoUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private StageSlider stageSlider;
    [SerializeField] private StageDatabase stageDB;

    [Header("UI")]
    [SerializeField] private TMP_Text stageNumberText;     // 상단 배너
    [SerializeField] private TMP_Text stageDescText;       // 하단 설명 박스

    private void OnEnable()
    {
        if (stageSlider != null)
            stageSlider.OnIndexChanged += HandleIndexChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (stageSlider != null)
            stageSlider.OnIndexChanged -= HandleIndexChanged;
    }

    private void HandleIndexChanged(int newIndex) => Refresh();

    public void Refresh()
    {
        if (stageSlider == null) return;

        int index = stageSlider.CurrentIndex;

        // stageId 결정(우선: stageDB, fallback: slider mapping)
        int stageId = stageSlider.GetSelectedStageId();
        StageData data = null;

        if (stageDB != null && stageDB.stages != null && index >= 0 && index < stageDB.stages.Length)
        {
            data = stageDB.stages[index];
            stageId = data.stageID;
        }

        if (stageNumberText != null)
            stageNumberText.text = $"STAGE {stageId}";

        if (stageDescText != null)
            stageDescText.text = (data != null) ? BuildDesc(data) : "스테이지 데이터가 연결되지 않았습니다.";
    }

    private string BuildDesc(StageData s)
    {
        var sb = new StringBuilder();

        // 목표
        if (s.useCollectGoal && s.collectTargets != null && s.collectTargets.Length > 0)
        {
            sb.Append("목표: ");
            for (int i = 0; i < s.collectTargets.Length; i++)
            {
                var t = s.collectTargets[i];
                sb.Append($"Gem {t.gemType} x{t.target}");
                if (i < s.collectTargets.Length - 1) sb.Append(", ");
            }
            sb.AppendLine();

            if (s.requirePassScore)
                sb.AppendLine($"추가: 점수 {s.targetScore}");
        }
        else
        {
            sb.AppendLine($"목표 점수: {s.targetScore}");
        }

        sb.AppendLine($"이동 횟수: {s.maxMoves}");

        // 장애물
        if (s.useObstacles)
        {
            int iceCount = 0;
            if (s.iceCage != null)
            {
                for (int i = 0; i < s.iceCage.Length; i++)
                    if (s.iceCage[i] != 0) iceCount++;
            }
            else
            {
                iceCount = s.obstacleCount;
            }

            sb.AppendLine($"장애물: ICE ({iceCount})");
        }

        return sb.ToString().TrimEnd();
    }
}
