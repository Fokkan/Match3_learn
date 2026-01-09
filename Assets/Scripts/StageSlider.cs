using System;
using System.Collections;
using UnityEngine;

public class StageSlider : MonoBehaviour
{
    [Header("연결 설정")]
    [SerializeField] private RectTransform[] carts;
    [SerializeField] private AudioClip moveSoundClip;

    [Header("설정")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float posY = 150f;
    [SerializeField] private float offScreenPos = 1000f;

    [Header("Stage Id Mapping (Optional)")]
    [Tooltip("carts 인덱스 -> 실제 stageId 매핑. 비우면 (index+1)")]
    [SerializeField] private int[] stageIds;

    private AudioSource audioSource;
    private int currentIndex = 0;
    private bool isMoving = false;

    // ===== Public API =====
    public int CurrentIndex => currentIndex;
    public bool IsMoving => isMoving;

    /// <summary>슬라이더 인덱스가 바뀔 때 호출</summary>
    public event Action<int> OnIndexChanged;

    /// <summary>현재 선택 인덱스의 stageId 반환 (stageIds가 있으면 우선)</summary>
    public int GetSelectedStageId()
    {
        if (carts == null || carts.Length == 0) return 1;

        if (stageIds != null && stageIds.Length == carts.Length)
            return stageIds[Mathf.Clamp(currentIndex, 0, carts.Length - 1)];

        return Mathf.Clamp(currentIndex, 0, carts.Length - 1) + 1;
    }

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = moveSoundClip;
        audioSource.playOnAwake = false;
        audioSource.loop = true;

        // 저장된 SFX 볼륨 즉시 적용
        audioSource.volume = PlayerPrefs.GetFloat("SFX_VOL", 1f);
    }

    private void Start()
    {
        // 시작 배치 보정(안전)
        SnapToIndex(currentIndex);
        OnIndexChanged?.Invoke(currentIndex);
    }

    // ===== Buttons =====
    public void ShowNextStage()
    {
        if (!CanSlide()) return;
        if (isMoving || currentIndex >= carts.Length - 1) return;

        StartCoroutine(SlideRoutine(currentIndex, currentIndex + 1, isNext: true));
    }

    public void ShowPrevStage()
    {
        if (!CanSlide()) return;
        if (isMoving || currentIndex <= 0) return;

        StartCoroutine(SlideRoutine(currentIndex, currentIndex - 1, isNext: false));
    }

    // ===== Internals =====
    private bool CanSlide()
    {
        if (carts == null || carts.Length == 0)
        {
            Debug.LogWarning("[StageSlider] carts가 비었습니다.");
            return false;
        }
        return true;
    }

    private void SnapToIndex(int index)
    {
        index = Mathf.Clamp(index, 0, carts.Length - 1);

        for (int i = 0; i < carts.Length; i++)
        {
            if (carts[i] == null) continue;

            if (i == index)
                carts[i].anchoredPosition = new Vector2(0f, posY);
            else
                carts[i].anchoredPosition = new Vector2((i < index) ? -offScreenPos : offScreenPos, posY);
        }
    }

    private IEnumerator SlideRoutine(int fromIndex, int toIndex, bool isNext)
    {
        isMoving = true;

        if (audioSource != null && moveSoundClip != null)
            audioSource.Play();

        RectTransform outgoing = carts[fromIndex];
        RectTransform incoming = carts[toIndex];

        // incoming 시작 위치 세팅
        float outTargetX = isNext ? offScreenPos : -offScreenPos;
        float inStartX = isNext ? -offScreenPos : offScreenPos;

        Vector2 outStart = new Vector2(0f, posY);
        Vector2 outEnd = new Vector2(outTargetX, posY);
        Vector2 inStart = new Vector2(inStartX, posY);
        Vector2 inEnd = new Vector2(0f, posY);

        if (incoming != null) incoming.anchoredPosition = inStart;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // SmoothStep
            float curve = t * t * (3f - 2f * t);

            if (outgoing != null) outgoing.anchoredPosition = Vector2.Lerp(outStart, outEnd, curve);
            if (incoming != null) incoming.anchoredPosition = Vector2.Lerp(inStart, inEnd, curve);

            yield return null;
        }

        if (outgoing != null) outgoing.anchoredPosition = outEnd;
        if (incoming != null) incoming.anchoredPosition = inEnd;

        // 인덱스 확정 + 이벤트
        currentIndex = toIndex;
        OnIndexChanged?.Invoke(currentIndex);

        if (audioSource != null) audioSource.Stop();
        isMoving = false;
    }
}
