using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Stage selection slider controller (Overlap Stack Version)
/// - Assumes all stage cart images are stacked at the same position under cartContainer.
/// - Does NOT move the container.
/// - Animates only TWO items (current + next) to preserve "single cart slides" presentation.
/// </summary>
public class StageSlider : MonoBehaviour
{
    [Header("Slider Setup")]
    [SerializeField] private RectTransform cartContainer;
    [SerializeField] private float slideOffsetX = 1000f;   // 화면에서 밀려나가는 거리 (당신 케이스: 1000 권장)
    [SerializeField] private float slideDuration = 0.25f;

    [Header("Direction (Visual)")]
    [SerializeField] private bool nextSlidesToRight = true; // "다음(오른쪽 화살표)" 누르면 오른쪽으로 밀리는 연출

    [Header("Stage Range")]
    [SerializeField] private int minIndex = 0;
    [SerializeField] private int maxIndex = 5; // 자식 수에 따라 런타임에서 자동 보정

    [Header("Audio")]
    [SerializeField] private AudioClip moveSfx;

    private int currentIndex = 0;
    private bool isMoving = false;

    private RectTransform[] stageItems = Array.Empty<RectTransform>();
    private Vector2 centerPos; // 겹쳐있는 기준 위치(센터)
    private AudioSource audioSource;

    public event Action<int> OnIndexChanged;

    public int CurrentIndex => currentIndex;
    public bool IsMoving => isMoving;

    private void Awake()
    {
        if (cartContainer == null)
            cartContainer = GetComponent<RectTransform>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        CacheStageItems();
        FixRangeByChildren();
        ClampIndex();

        CacheCenterPos();
        ApplyImmediateState();
        RaiseIndexChanged();
    }

    private void CacheStageItems()
    {
        if (cartContainer == null)
        {
            stageItems = Array.Empty<RectTransform>();
            return;
        }

        int count = cartContainer.childCount;
        stageItems = new RectTransform[count];
        for (int i = 0; i < count; i++)
            stageItems[i] = cartContainer.GetChild(i) as RectTransform;
    }

    private void FixRangeByChildren()
    {
        if (stageItems == null || stageItems.Length == 0) return;

        int last = stageItems.Length - 1;
        minIndex = Mathf.Clamp(minIndex, 0, last);
        maxIndex = Mathf.Clamp(maxIndex, minIndex, last);
    }

    private void ClampIndex()
    {
        if (maxIndex < minIndex) maxIndex = minIndex;
        currentIndex = Mathf.Clamp(currentIndex, minIndex, maxIndex);
    }

    private void CacheCenterPos()
    {
        // 겹쳐있는 기준 위치는 "현재 선택된 아이템의 anchoredPosition"을 센터로 사용
        if (stageItems == null || stageItems.Length == 0)
        {
            centerPos = Vector2.zero;
            return;
        }

        int idx = Mathf.Clamp(currentIndex, 0, stageItems.Length - 1);
        centerPos = stageItems[idx].anchoredPosition;
    }

    private void ApplyImmediateState()
    {
        if (stageItems == null || stageItems.Length == 0) return;

        for (int i = 0; i < stageItems.Length; i++)
        {
            if (stageItems[i] == null) continue;

            stageItems[i].anchoredPosition = centerPos;
            stageItems[i].gameObject.SetActive(i == currentIndex);
        }
    }

    public void JumpToIndex(int index)
    {
        if (isMoving) return;

        int target = Mathf.Clamp(index, minIndex, maxIndex);
        if (target == currentIndex) return;

        currentIndex = target;
        ApplyImmediateState();
        RaiseIndexChanged();
    }

    public void ShowNextStage()
    {
        if (isMoving) return;
        if (currentIndex >= maxIndex) return;

        SlideToIndex(currentIndex + 1);
    }

    public void ShowPrevStage()
    {
        if (isMoving) return;
        if (currentIndex <= minIndex) return;

        SlideToIndex(currentIndex - 1);
    }

    private void SlideToIndex(int newIndex)
    {
        newIndex = Mathf.Clamp(newIndex, minIndex, maxIndex);
        if (newIndex == currentIndex) return;

        if (stageItems == null || stageItems.Length == 0)
        {
            currentIndex = newIndex;
            RaiseIndexChanged();
            return;
        }

        // 시각적 이동 방향 결정:
        // - "다음"이면 기본적으로 오른쪽으로 밀리게(nextSlidesToRight=true)
        // - "이전"은 반대 방향
        int indexDirection = (newIndex > currentIndex) ? 1 : -1; // 다음:+1 / 이전:-1
        int visualBase = nextSlidesToRight ? 1 : -1;
        float visualSign = indexDirection * visualBase; // +면 오른쪽으로, -면 왼쪽으로 밀림

        RectTransform from = stageItems[currentIndex];
        RectTransform to = stageItems[newIndex];

        if (from == null || to == null)
        {
            currentIndex = newIndex;
            ApplyImmediateState();
            RaiseIndexChanged();
            return;
        }

        // 다음 아이템 활성화 + 진입 위치 세팅(반대편에서 들어옴)
        to.gameObject.SetActive(true);
        to.anchoredPosition = centerPos + new Vector2(-visualSign * slideOffsetX, 0f);

        // 현재 아이템은 센터에서 시작하도록 보정
        from.anchoredPosition = centerPos;

        if (audioSource != null && moveSfx != null)
            audioSource.PlayOneShot(moveSfx);

        StopAllCoroutines();
        StartCoroutine(SlideCoroutine(from, to, newIndex, visualSign));
    }

    private IEnumerator SlideCoroutine(RectTransform from, RectTransform to, int newIndex, float visualSign)
    {
        isMoving = true;

        Vector2 fromStart = centerPos;
        Vector2 fromEnd = centerPos + new Vector2(visualSign * slideOffsetX, 0f);

        Vector2 toStart = centerPos + new Vector2(-visualSign * slideOffsetX, 0f);
        Vector2 toEnd = centerPos;

        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / slideDuration);

            if (from != null) from.anchoredPosition = Vector2.Lerp(fromStart, fromEnd, a);
            if (to != null) to.anchoredPosition = Vector2.Lerp(toStart, toEnd, a);

            yield return null;
        }

        // 마무리 스냅
        if (to != null) to.anchoredPosition = toEnd;

        // 이전 아이템 비활성화(겹침 유지)
        if (from != null)
        {
            from.gameObject.SetActive(false);
            from.anchoredPosition = centerPos; // 다음 전환 대비 원위치
        }

        currentIndex = newIndex;
        isMoving = false;

        RaiseIndexChanged();
    }

    private void RaiseIndexChanged()
    {
        OnIndexChanged?.Invoke(currentIndex);
    }

    /// <summary>
    /// Stage ID is 1-based (Index 0 => Stage 1)
    /// </summary>
    public int GetSelectedStageId()
    {
        return CurrentIndex + 1;
    }
}
