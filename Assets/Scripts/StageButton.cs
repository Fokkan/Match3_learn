using UnityEngine;
using UnityEngine.SceneManagement;

public class StageButton : MonoBehaviour
{
    public int stageID;

    public void OnClickStage()

    {
        PlayerPrefs.SetInt("CurrentStage", stageID);
        SceneManager.LoadScene("GameScene");
    }
}
