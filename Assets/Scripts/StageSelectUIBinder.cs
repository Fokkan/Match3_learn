using UnityEngine;

public class StageSelectUIBinder : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private StageSlider stageSlider;
    [SerializeField] private StageDatabase stageDB;
    [SerializeField] private StageInfoUI stageInfoUI;

    private int lastIndex = -1;

    private void Awake()
    {
        // 인스펙터 연결이 빠졌을 때만 자동 탐색(에러 방지용)
        if (stageSlider == null) stageSlider = Object.FindFirstObjectByType<StageSlider>();
        if (stageInfoUI == null) stageInfoUI = Object.FindFirstObjectByType<StageInfoUI>();
    }

    private void Start()
    {
        ForceRefresh();
    }

    private void Update()
    {
        if (stageSlider == null || stageDB == null || stageInfoUI == null) return;

        int idx = stageSlider.CurrentIndex;
        if (idx == lastIndex) return;

        lastIndex = idx;
        ApplyIndex(idx);
    }

    public void ForceRefresh()
    {
        if (stageSlider == null || stageDB == null || stageInfoUI == null) return;

        lastIndex = stageSlider.CurrentIndex;
        ApplyIndex(lastIndex);
    }

    private void ApplyIndex(int index)
    {
        if (stageDB == null || stageInfoUI == null) return;

        StageData stage = stageDB.GetStageByIndex(index);
        stageInfoUI.SetStage(stage);
    }

}
