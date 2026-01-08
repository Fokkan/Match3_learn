using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StageSlider : MonoBehaviour
{
    [Header("연결 설정")]
    public RectTransform[] carts;
    public AudioClip moveSoundClip;

    [Header("설정")]
    public float duration = 0.5f;
    public float posY = 150f;
    public float offScreenPos = 1000f;

    private AudioSource audioSource;
    private int currentIndex = 0;
    private bool isMoving = false;
    [Header("Stage Id Mapping (Optional)")]
    // carts 인덱스 -> 실제 stageId 매핑이 필요하면 사용 (예: [1,2,3,4,5,6])
    public int[] stageIds;


    public bool IsMoving => isMoving;

    public int GetSelectedStageId()
    {
        // stageIds가 세팅되어 있고 길이가 carts와 같으면 그걸 우선
        if (stageIds != null && stageIds.Length == carts.Length)
            return stageIds[currentIndex];

        // 기본은 index+1을 stageId로 사용
        return currentIndex + 1;
    }

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = moveSoundClip;
        audioSource.playOnAwake = false;
        audioSource.loop = true;

        // ⭐ [추가] 시작하자마자 저장된 SFX 볼륨을 불러와서 즉시 적용!
        // 이 한 줄이 없어서 설정창을 열기 전까지 소리가 컸던 것입니다.
        float savedSFX = PlayerPrefs.GetFloat("SFX_VOL", 1f);
        audioSource.volume = savedSFX;
    }

    // ... (나머지 SlideRoutine 등 기존 코드와 동일)
    public void ShowNextStage()
    {
        if (isMoving || currentIndex >= carts.Length - 1) return;
        StartCoroutine(SlideRoutine(currentIndex, currentIndex + 1, true));
    }

    public void ShowPrevStage()
    {
        if (isMoving || currentIndex <= 0) return;
        StartCoroutine(SlideRoutine(currentIndex, currentIndex - 1, false));
    }

    IEnumerator SlideRoutine(int fromIndex, int toIndex, bool isNext)
    {
        isMoving = true;
        if (audioSource != null && moveSoundClip != null) audioSource.Play();

        RectTransform outgoing = carts[fromIndex];
        RectTransform incoming = carts[toIndex];
        float elapsed = 0f;

        float outTargetX = isNext ? offScreenPos : -offScreenPos;
        float inStartX = isNext ? -offScreenPos : offScreenPos;

        Vector2 outStart = new Vector2(0, posY);
        Vector2 outEnd = new Vector2(outTargetX, posY);
        Vector2 inStart = new Vector2(inStartX, posY);
        Vector2 inEnd = new Vector2(0, posY);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curve = t * t * (3f - 2f * t);

            outgoing.anchoredPosition = Vector2.Lerp(outStart, outEnd, curve);
            incoming.anchoredPosition = Vector2.Lerp(inStart, inEnd, curve);
            yield return null;
        }
        
        outgoing.anchoredPosition = outEnd;
        incoming.anchoredPosition = inEnd;
        currentIndex = toIndex;

        if (audioSource != null) audioSource.Stop();
        isMoving = false;
    }

    public int CurrentIndex => currentIndex;
    public int CurrentStageId => currentIndex + 1; // 1-based
}