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
        if (stageSlider == null) stageSlider = Object.FindFirstObjectByType<StageSlider>();
        if (stageInfoUI == null) stageInfoUI = Object.FindFirstObjectByType<StageInfoUI>();

        // 핵심: 인스펙터에서 stageDB가 비었으면, StageEnterButton에 물려있는 DB를 가져온다
        if (stageDB == null)
        {
            var enter = Object.FindFirstObjectByType<StageEnterButton>();
            if (enter != null) stageDB = enter.StageDB;
        }
    }

    private void OnEnable()
    {
        if (stageSlider != null)
            stageSlider.OnIndexChanged += HandleIndexChanged;
    }

    private void OnDisable()
    {
        if (stageSlider != null)
            stageSlider.OnIndexChanged -= HandleIndexChanged;
    }

    private void Start()
    {
        ForceRefresh();
    }
    private void Update()
    {
        // stageDB 체크 제거: StageInfoUI 내부 stageDB를 사용
        if (stageSlider == null || stageInfoUI == null) return;

        int idx = stageSlider.CurrentIndex;
        if (idx == lastIndex) return;

        lastIndex = idx;
        stageInfoUI.SetStageByIndex(idx);
    }

    public void ForceRefresh()
    {
        if (stageSlider == null || stageInfoUI == null) return;

        lastIndex = stageSlider.CurrentIndex;
        stageInfoUI.SetStageByIndex(lastIndex);
    }


    private void HandleIndexChanged(int idx)
    {
        if (stageDB == null || stageInfoUI == null) return;
        if (idx == lastIndex) return;

        lastIndex = idx;
        ApplyIndex(idx);
    }

    private void ApplyIndex(int index)
    {
        if (stageInfoUI == null) return;
        stageInfoUI.SetStageByIndex(index);
    }

}
