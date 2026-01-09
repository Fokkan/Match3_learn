using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필요합니다.

public class ReturnButtonHandler : MonoBehaviour
{
    // 돌아갈 씬 이름을 여기에 적으세요.
    public string targetSceneName = "Game Start";

    public void BackToPreviousScene()
    {
        // 씬을 불러옵니다.
        SceneManager.LoadScene(targetSceneName);
    }
}