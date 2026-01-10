using UnityEngine;
using UnityEngine.SceneManagement;

public class StageEnterButton : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private StageSlider stageSlider;
    [SerializeField] private StageDatabase stageDB;

    [Header("Scene")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [Header("Save Key")]
    [SerializeField] private string selectedStageKey = "SelectedStageId";

    public void EnterSelectedStage()
    {
        if (stageSlider == null || stageDB == null)
        {
            Debug.LogError("[StageEnterButton] stageSlider / stageDB reference missing.");
            return;
        }

        // 슬라이드 중 클릭 진입 방지
        if (stageSlider.IsMoving) return;

        // StageSlider는 0-based index이지만, 실제 저장/진입은 stageID(1-based)로 통일
        int stageId = stageSlider.GetSelectedStageId();
        StageData stage = stageDB.GetStageById(stageId);

        if (stage == null)
        {
            Debug.LogError($"[StageEnterButton] StageData not found for stageId={stageId} (index={stageSlider.CurrentIndex})");
            return;
        }

        PlayerPrefs.SetInt(selectedStageKey, stage.stageID);
        PlayerPrefs.Save();

        SceneManager.LoadScene(gameplaySceneName);
    }

}
