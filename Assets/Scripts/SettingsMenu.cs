using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

#if DOTWEEN
using DG.Tweening;
#endif

public class SettingsMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject settingsWindow;

    [Header("Audio")]
    public AudioMixer masterMixer; // Gameplay에서 실제로 쓰는 Mixer를 넣어야 함

    [Header("Scene")]
    [SerializeField] private string stageSelectSceneName = "Game Stage";

    [Header("Gameplay Option")]
    [SerializeField] private bool pauseGameOnOpen = true;

    private void Awake()
    {
        // 씬 로드/프리팹 상태와 상관없이 시작 시 무조건 닫힘 보장
        if (settingsWindow != null)
            settingsWindow.SetActive(false);

        // 혹시 이전 씬에서 timeScale이 꼬였을 가능성까지 초기화
        Time.timeScale = 1f;
    }

    // 설정창 열기 (톱니 버튼)
    public void OpenSettings()
    {
        if (settingsWindow != null)
            settingsWindow.SetActive(true);

        if (pauseGameOnOpen)
            Time.timeScale = 0f;
    }

    // 설정창 닫기 (뒤로가기 버튼)
    public void CloseSettings()
    {
        if (pauseGameOnOpen)
            Time.timeScale = 1f;

        if (settingsWindow != null)
            settingsWindow.SetActive(false);
    }

    // 전원 버튼: 스테이지 선택 씬으로 복귀
    public void GoStageSelect()
    {
        Time.timeScale = 1f;

#if DOTWEEN
        DG.Tweening.DOTween.KillAll(true);
#endif

        // 혹시 남아있는 Gameplay 매니저가 DDOL이면 제거(오디오 제외)
        var stageManagers = Object.FindObjectsByType<StageManager>(FindObjectsSortMode.None);
        foreach (var sm in stageManagers) Destroy(sm.gameObject);

        var boardManagers = Object.FindObjectsByType<BoardManager>(FindObjectsSortMode.None);
        foreach (var bm in boardManagers) Destroy(bm.gameObject);

        SceneManager.LoadScene("Game Stage");

    }

    // BGM 조절
    public void SetBGM(float volume)
    {
        if (masterMixer == null) return;

        volume = Mathf.Clamp(volume, 0.0001f, 1f);
        masterMixer.SetFloat("BGM_Vol", Mathf.Log10(volume) * 20f);
    }

    // SFX 조절
    public void SetSFX(float volume)
    {
        if (masterMixer == null) return;

        volume = Mathf.Clamp(volume, 0.0001f, 1f);
        masterMixer.SetFloat("SFX_Vol", Mathf.Log10(volume) * 20f);
    }
}
