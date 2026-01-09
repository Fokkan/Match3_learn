using UnityEngine;
using UnityEngine.SceneManagement;

public class StageEnterButton : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private StageSlider stageSlider;

    [Header("Scene")]
    [SerializeField] private string gameplaySceneName = "Game Play";
    [SerializeField] private string selectedStageKey = "SelectedStageIndex";

    public void EnterSelectedStage()
    {
        if (stageSlider == null)
        {
            Debug.LogError("[StageEnterButton] stageSlider is NULL");
            return;
        }

        int index = stageSlider.CurrentIndex; // 0-based
        PlayerPrefs.SetInt(selectedStageKey, index);
        PlayerPrefs.Save();

        Debug.Log($"[StageEnterButton] Enter -> index={index}, scene={gameplaySceneName}");
        var all = FindObjectsOfType<AudioSource>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var src = all[i];
            if (src == null) continue;
            if (src.isPlaying && src.loop)
                src.Stop();
        }
        SceneManager.LoadScene(gameplaySceneName);
    }
}
