using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public GameObject transitionCanvas;
    public Image pageImage;
    public Animator pageAnimator;

    // 중복 실행 방지용 변수
    private bool isTransitioning = false;

    public void StartTransition()
    {
        // 이미 연출 중이면 다시 실행하지 않음
        if (isTransitioning) return;

        isTransitioning = true;
        StartCoroutine(TransitionRoutine());
    }

    IEnumerator TransitionRoutine()
    {
        yield return new WaitForEndOfFrame();

        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();
        pageImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

        transitionCanvas.SetActive(true);
        DontDestroyOnLoad(transitionCanvas);
        DontDestroyOnLoad(gameObject);

        // 씬 이동
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Game Stage");
        while (!asyncLoad.isDone) { yield return null; }

        yield return new WaitForSeconds(0.1f);

        // 연출 실행
        if (pageAnimator != null)
        {
            pageAnimator.SetTrigger("Flip");
        }

        // 연출이 완전히 끝날 때까지 넉넉히 대기 (1.5초)
        yield return new WaitForSeconds(1.5f);

        // 정리
        Destroy(transitionCanvas);
        Destroy(gameObject);
    }
}