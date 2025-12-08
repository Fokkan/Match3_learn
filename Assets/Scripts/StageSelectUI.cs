using UnityEngine;

public class StageSelectUI : MonoBehaviour
{
    [Header("Stage Select UI")]
    public GameObject stageSelectPanel;   // StageSelectPanel 오브젝트 연결

    // 버튼에서 이 함수에 인덱스를 넘겨서 호출 (0,1,2...)
    public void SelectStage(int stageIndex)
    {
        // 1) 스테이지 정보 적용
        if (StageManager.Instance != null)
        {
            StageManager.Instance.SelectStage(stageIndex);
        }

        // 2) 패널 끄기
        if (stageSelectPanel != null)
        {
            stageSelectPanel.SetActive(false);
        }
    }
}
