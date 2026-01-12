using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ResultPopupUi : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panel;

    [Header("Texts (Optional allowed)")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text scoreSummaryText;
    [SerializeField] private TMP_Text goalSummaryText;
    [SerializeField] private TMP_Text movesLeftText;
    [SerializeField] private TMP_Text bonusScoreText;
    [SerializeField] private TMP_Text finalScoreText;

    [Header("Stars")]
    [SerializeField] private List<Image> stars = new List<Image>();
    [SerializeField] private Sprite starOnSprite;
    [SerializeField] private Sprite starOffSprite;

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button stageSelectButton;

    [Header("Scene Names (fallback only)")]
    [SerializeField] private string gameplaySceneName = "Game Play";
    [SerializeField] private string stageSelectSceneName = "Game Stage";

    [Header("Stage Key (PlayerPrefs)")]
    [SerializeField] private string selectedStageKey = "SelectedStageId";

    private bool _cachedIsWin;
    private bool _cachedHasNext;
    private bool _transitionLock; //  Next가 +2 되는 케이스(중복 호출) 방지

    private void Awake()
    {
        BindButtonsHard();
    }

    private void OnEnable()
    {
        // 인스펙터/런타임에서 버튼이 바뀌었어도 항상 여기서 한 번 더 고정
        BindButtonsHard();
    }

    /// <summary>
    ///  핵심: 인스펙터(OnClick)에 걸린 것 포함 "전부 제거" 후, 우리가 원하는 핸들러만 1개씩 붙인다.
    /// </summary>
    private void BindButtonsHard()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryClicked);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }

        if (stageSelectButton != null)
        {
            stageSelectButton.onClick.RemoveAllListeners();
            stageSelectButton.onClick.AddListener(OnStageSelectClicked);
        }
    }

    public void Show(
        bool isWin,
        int earnedStars,
        int targetScore,
        int score,
        int movesLeft,
        int maxMoves,
        int bonusScore,
        int finalScore,
        bool hasNextStage
    )
    {
        _transitionLock = false;
        _cachedIsWin = isWin;
        _cachedHasNext = hasNextStage;

        if (root != null) root.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // 타이틀
        if (titleText != null)
            titleText.text = isWin ? "STAGE CLEAR!" : "FAILED";

        // 텍스트(연결된 것만 갱신)
        if (goalSummaryText != null) goalSummaryText.text = $"TARGET: {targetScore:n0}";
        if (scoreSummaryText != null) scoreSummaryText.text = $"YOUR SCORE: {score:n0}";
        if (movesLeftText != null) movesLeftText.text = $"MOVES: {movesLeft}/{maxMoves}";
        if (bonusScoreText != null) bonusScoreText.text = $"+{bonusScore:n0}";
        if (finalScoreText != null) finalScoreText.text = $"FINAL: {finalScore:n0}";

        // 별
        int count = (stars != null) ? stars.Count : 0;
        for (int i = 0; i < count; i++)
        {
            if (stars[i] == null) continue;

            bool on = (i < earnedStars);

            if (starOnSprite != null && starOffSprite != null)
                stars[i].sprite = on ? starOnSprite : starOffSprite;

            var c = stars[i].color;
            c.a = on ? 1f : 0.25f;
            stars[i].color = c;
        }

        // 버튼 정책
        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(true);
            retryButton.interactable = true;
        }

        if (nextButton != null)
        {
            bool showNext = isWin && hasNextStage;
            nextButton.gameObject.SetActive(showNext);
            nextButton.interactable = showNext;
        }

        if (stageSelectButton != null)
        {
            stageSelectButton.gameObject.SetActive(true);
            stageSelectButton.interactable = true;
        }
    }

    public void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        if (root != null) root.SetActive(false);
    }

    private void OnRetryClicked()
    {
        if (_transitionLock) return;
        _transitionLock = true;

        Time.timeScale = 1f;

        //  씬 리로드 금지: StageManager로 "현재 스테이지" 재로드
        if (StageManager.Instance != null)
        {
            HideImmediate();

            int stageId = PlayerPrefs.GetInt(selectedStageKey, 1);
            StageManager.Instance.LoadStageById(stageId);
            return;
        }

        // (폴백) StageManager가 없으면 씬 리로드
        SceneManager.LoadScene(gameplaySceneName);
    }

    private void OnNextClicked()
    {
        Debug.Log($"[CLICK] Next pressed frame={Time.frameCount}");

        if (_transitionLock) return;
        _transitionLock = true;

        if (!_cachedIsWin || !_cachedHasNext)
        {
            _transitionLock = false;
            return;
        }

        Time.timeScale = 1f;

        //  씬 리로드 금지: StageManager로 다음 스테이지(정확히 +1) 로드
        if (StageManager.Instance != null)
        {
            if (!StageManager.Instance.HasNextStage())
            {
                _transitionLock = false;
                return;
            }

            HideImmediate();

            int nextIndex = StageManager.Instance.currentStageIndex + 1;
            StageManager.Instance.LoadStageByIndex(nextIndex);
            return;
        }

        // (폴백) StageManager가 없으면 PlayerPrefs +1 후 씬 리로드
        int current = PlayerPrefs.GetInt(selectedStageKey, 1);
        PlayerPrefs.SetInt(selectedStageKey, current + 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameplaySceneName);
    }

    private void OnStageSelectClicked()
    {
        if (_transitionLock) return;
        _transitionLock = true;

        Time.timeScale = 1f;
        SceneManager.LoadScene(stageSelectSceneName);
    }
}
