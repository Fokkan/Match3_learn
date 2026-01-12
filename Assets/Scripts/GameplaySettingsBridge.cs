using UnityEngine;
using UnityEngine.SceneManagement;

#if DOTWEEN
using DG.Tweening;
#endif

public class GameplaySettingsBridge : MonoBehaviour
{
    [Header("Optional: Close target (SettingWindow root)")]
    [SerializeField] private GameObject settingWindowRoot; // 닫을 대상(없으면 이 오브젝트를 끔)

    [Header("Options")]
    [SerializeField] private string stageSelectSceneName = "Game Stage";

    // Resume(닫기) 버튼에 연결
    public void OnClickResume()
    {
        Time.timeScale = 1f;

        if (settingWindowRoot != null) settingWindowRoot.SetActive(false);
        else gameObject.SetActive(false);
    }

    // Stage Select로 돌아가기 버튼에 연결
    public void OnClickGoStageSelect()
    {
        Time.timeScale = 1f;

#if DOTWEEN
        DOTween.KillAll(true);
#endif

        SceneManager.LoadScene(stageSelectSceneName);
    }
}
