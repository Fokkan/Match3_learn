using UnityEngine;

public class StageSelectUI : MonoBehaviour
{
    [Header("Stage Select UI")]
    public GameObject stageSelectPanel;   // 연결돼 있으면 패널 끄고, 아니면 자기 자신 끔

    // 선택한 인덱스(0-based)를 저장하고 싶으면 필드가 필요함
    private int selectedStageIndex = -1;

    // 버튼에서 stageIndex(0-based)로 호출
    public void SelectStage(int stageIndex)
    {
        selectedStageIndex = stageIndex;

        if (StageManager.Instance != null)
        {
            // 핵심: stageIndex(0-based)면 SelectStage(stageId) 쓰면 꼬일 수 있으니
            // "LoadStageByIndex"로 고정 (StageManager.cs에 이미 존재함)
            StageManager.Instance.LoadStageByIndex(stageIndex);
        }
        else
        {
            Debug.LogWarning("[StageSelectUI] StageManager.Instance is null.");
        }

        if (stageSelectPanel != null) stageSelectPanel.SetActive(false);
        else gameObject.SetActive(false);
    }
}
