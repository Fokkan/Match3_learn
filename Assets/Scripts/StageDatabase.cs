using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Match3/Stage Database", fileName = "StageDB")]
public class StageDatabase : ScriptableObject
{
    // YAML: stages
    public List<StageData> stages = new List<StageData>();

    public int Count => stages == null ? 0 : stages.Count;

    // 0-based index (StageSlider.CurrentIndex와 동일 기준)
    public StageData GetStageByIndex(int index)
    {
        if (stages == null || stages.Count == 0) return null;
        if (index < 0 || index >= stages.Count) return null;
        return stages[index];
    }

    // 1-based stageID
    public StageData GetStageById(int stageId)
    {
        if (stages == null) return null;

        for (int i = 0; i < stages.Count; i++)
        {
            var s = stages[i];
            if (s != null && s.stageID == stageId) return s;
        }
        return null;
    }

    public int GetIndexById(int stageId)
    {
        if (stages == null) return -1;

        for (int i = 0; i < stages.Count; i++)
        {
            var s = stages[i];
            if (s != null && s.stageID == stageId) return i;
        }
        return -1;
    }
}
