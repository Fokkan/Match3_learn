using UnityEngine;
using UnityEngine.SceneManagement;

public class CartHitButton : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private StageSlider stageSlider;

    [Header("Scene")]
    [SerializeField] private string gameplaySceneName = "SampleScene"; // 너 메인 게임 씬 이름으로
    [SerializeField] private string selectedStageKey = "SelectedStageId";

    public void EnterSelectedStage()
    {
        if (stageSlider == null)
        {
            Debug.LogError("[CartHitButton] StageSlider reference missing.");
            return;
        }

        if (stageSlider.IsMoving) return; // 슬라이드 중 클릭 방지

        int stageId = stageSlider.GetSelectedStageId();
        PlayerPrefs.SetInt(selectedStageKey, stageId);
        PlayerPrefs.Save();

        SceneManager.LoadScene(gameplaySceneName);

    }

}
