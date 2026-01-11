using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class StageEnterButton : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private StageSlider stageSlider;
    [SerializeField] private StageDatabase stageDB;

    [Header("Scene")]
    [SerializeField] private string gameplaySceneName = "Game Play";

    [Header("Save Key")]
    [SerializeField] private string selectedStageKey = "SelectedStageId";

    [Header("BGM (Stage Select)")]
    [SerializeField] private bool destroyStageSelectBgmOnEnter = true;
    [SerializeField] private string stageSelectBgmObjectName = "BGM_Player";


    // StageSelectUIBinder가 StageDB를 가져갈 수 있도록 공개
    public StageDatabase StageDB => stageDB;

    public void EnterSelectedStage()
    {
        if (stageSlider == null) stageSlider = Object.FindFirstObjectByType<StageSlider>();
        if (stageDB == null)
        {
            Debug.LogError("[StageEnterButton] stageDB reference missing.");
            return;
        }

        int index = stageSlider != null ? stageSlider.CurrentIndex : 0;
        StageData stage = stageDB.GetStageByIndex(index);

        if (stage == null)
        {
            Debug.LogError($"[StageEnterButton] StageData is null at index={index}");
            return;
        }

        PlayerPrefs.SetInt(selectedStageKey, stage.stageID);
        PlayerPrefs.Save();

        if (destroyStageSelectBgmOnEnter)
        {
            DestroyStageSelectBgmPlayer();
            StartCoroutine(LoadGameplayNextFrame());
            return;
        }

        SceneManager.LoadScene(gameplaySceneName);

    }

    private void DestroyStageSelectBgmPlayer()
    {
        var go = GameObject.Find(stageSelectBgmObjectName);
        if (go != null)
        {
            Destroy(go);
        }
    }
    private IEnumerator LoadGameplayNextFrame()
    {
        // Destroy()가 실제 반영되도록 1프레임 대기
        yield return null;
        SceneManager.LoadScene(gameplaySceneName);
    }

}
