using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class BoardManager : MonoBehaviour
{
    #region Serialized Fields

    [Header("Camera Settings")]
    public Camera mainCamera;
    public float boardTopMargin = 2f;
    public float boardSideMargin = 0.5f;

    [Header("Board Plate")]
    public SpriteRenderer boardPlate;
    public SpriteRenderer boardGrid;
    public float boardPlatePadding = 0.5f;

    [Header("Board Plate Style")]
    public Color boardPlateColor = new Color(1f, 0.8f, 0.9f, 0.85f);
    public Color boardGridColor = new Color(1f, 1f, 1f, 0.9f);

    [Header("Board Size")]
    public int width = 8;
    public int height = 8;

    [Header("Gem Settings")]
    public GameObject gemPrefab;
    public Sprite[] gemSprites;

    [Header("Score Settings")]
    public TMP_Text scoreText;
    public int baseScorePerGem = 10;

    [Header("Effect Settings")]
    public float popDuration = 0.25f;
    public float fallWaitTime = 0.25f;
    public GameObject matchEffectPrefab;

    [Header("Sound Settings")]
    public AudioSource audioSource;
    public AudioClip swapClip;
    public AudioClip matchClip;
    public AudioClip shuffleClip;

    [Header("Shuffle Settings")]
    public TMP_Text shuffleText;
    public float shuffleMessageTime = 0.7f;
    public GameObject shuffleOverlay;
    public float shuffleTextYOffset = 0f;

    [Header("Animation Settings")]
    public float swapResolveDelay = 0.18f;
    public float specialExplodeDelay = 0.15f;

    [Header("Combo UI")]
    public TMP_Text comboText;
    public float comboShowTime = 0.6f;
    public float comboScaleFrom = 0.7f;
    public float comboScaleTo = 1.2f;

    [Header("Game Rule Settings")]
    public int targetScore = 500;
    public int defaultMaxMoves = 20;

    [Header("Game Rule UI")]
    public TMP_Text goalText;
    public TMP_Text movesText;
    public GameObject gameOverPanel;
    public TMP_Text resultText;

    [Header("Result Buttons")]
    public Button retryButton;
    public Button nextStageButton;

    [Header("Clear Summary UI")]
    public TMP_Text clearScoreText;
    public TMP_Text clearGoalText;
    public TMP_Text clearMovesLeftText;
    public TMP_Text clearBonusText;
    public TMP_Text clearFinalScoreText;

    [Header("Score Popup (Candy Style)")]
    public GameObject scorePopupPrefab;
    public RectTransform uiCanvasRect;
    public float popupDuration = 0.6f;
    public float popupMoveUp = 60f;
    public float popupScaleFrom = 0.6f;
    public float popupScaleTo = 1.05f;
    public Vector2 popupBaseOffset = new Vector2(0f, 20f);
    public float popupRandomOffsetX = 12f;
    public float popupRandomOffsetY = 6f;

    [Header("Hint Settings")]
    public float hintDelay = 3f; // 최소 3초 이상 권장

    #endregion

    #region Private Fields
    // ===== ICE Obstacle =====
    public enum ObstacleType { None, Ice }

    [Header("Obstacle - Ice")]
    public GameObject icePrefab;              // ICE 프리팹
    public List<Vector2Int> stage4IceCells;   // 스테이지4 얼음 위치

    private ObstacleType[,] obstacles;
    private GameObject[,] iceObjects;

    private Gem[,] gems;
    private Gem selectedGem = null;

    private int score = 0;
    private int maxMoves;
    private int movesLeft;

    private bool isGameOver = false;
    private bool isShuffling = false;
    private bool isAnimating = false;

    private float idleTimer = 0f;
    private Gem hintGemA = null;
    private Gem hintGemB = null;

    private int currentComboForPopup = 1;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (boardPlate != null) boardPlate.gameObject.SetActive(false);
        if (boardGrid != null) boardGrid.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (StageManager.Instance == null || StageManager.Instance.CurrentStage == null)
        {
            maxMoves = defaultMaxMoves;
            ResetState();

            gems = new Gem[width, height];
            GenerateBoard();
            InitIceArrays();
            ApplyIceForStage4();

            // 시작 1회 보드 검증(즉시 매치 제거 + 무브 확보)
            StartCoroutine(ShuffleRoutine(true));

            UpdateScoreUI();
            UpdateGoalUI();
            UpdateMovesUI();

            if (boardPlate != null) boardPlate.gameObject.SetActive(true);
            if (boardGrid != null) boardGrid.gameObject.SetActive(true);

            UpdateBoardPlate();
            AdjustCameraAndBoard();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            StartCoroutine(ShuffleRoutine(force: true));

        if (Input.GetKeyDown(KeyCode.R))
            RestartGame();
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("[TEST] Force ShuffleRoutine(true)");
            StartCoroutine(ShuffleRoutine(true));
        }

#if UNITY_EDITOR
        HandleDebugKeys();
#endif

        if (isGameOver) return;

        if (isAnimating || isShuffling || selectedGem != null)
        {
            idleTimer = 0f;
            return;
        }

        idleTimer += Time.deltaTime;

        if (idleTimer >= hintDelay)
            ShowHintIfPossible();
    }

    #endregion

    #region Board Generation / Camera

    private void GenerateBoard()
    {
        if (gemPrefab == null)
        {
            Debug.LogError("Gem Prefab이 BoardManager에 연결되지 않았습니다.");
            return;
        }

        if (gems == null || gems.GetLength(0) != width || gems.GetLength(1) != height)
            gems = new Gem[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int type = GetRandomTypeForInitial(x, y);

                GameObject obj = Instantiate(gemPrefab, Vector3.zero, Quaternion.identity, transform);

                var sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    if (gemSprites != null && gemSprites.Length > 0)
                        sr.sprite = gemSprites[type];
                    sr.color = Color.white;
                }

                Gem gem = obj.GetComponent<Gem>();
                gem.Init(this, x, y, type);

                gem.SnapToGrid();

                gems[x, y] = gem;
                obj.name = $"Gem ({x},{y})";
            }
        }
    }

    private void UpdateBoardPlate()
    {
        if (boardPlate == null || boardPlate.sprite == null)
            return;

        float cellSize = 1f;
        float gemsWidth = width * cellSize;
        float gemsHeight = height * cellSize;

        float targetWidth = gemsWidth + boardPlatePadding * 2f;
        float targetHeight = gemsHeight + boardPlatePadding * 2f;

        if (boardPlate != null && boardPlate.sprite != null)
        {
            boardPlate.drawMode = SpriteDrawMode.Sliced;
            boardPlate.size = new Vector2(targetWidth, targetHeight);
            boardPlate.transform.localPosition = Vector3.zero;
            boardPlate.color = boardPlateColor;
        }

        if (boardGrid != null && boardGrid.sprite != null)
        {
            boardGrid.drawMode = SpriteDrawMode.Tiled;
            boardGrid.size = new Vector2(targetWidth, targetHeight);
            boardGrid.transform.localPosition = Vector3.zero;
            boardGrid.color = boardGridColor;
        }
    }

    private void ClearBoard()
    {
        if (gems != null)
        {
            int w = gems.GetLength(0);
            int h = gems.GetLength(1);

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (gems[x, y] != null)
                    {
                        Destroy(gems[x, y].gameObject);
                        gems[x, y] = null;
                    }
                }
            }
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (boardPlate != null && child == boardPlate.transform) continue;
            if (boardGrid != null && child == boardGrid.transform) continue;

            if (child.GetComponent<Gem>() != null)
                Destroy(child.gameObject);
        }

        gems = null;
    }

    private int GetRandomTypeForInitial(int x, int y)
    {
        int typeCount = (gemSprites != null && gemSprites.Length > 0) ? gemSprites.Length : 5;

        int type = Random.Range(0, typeCount);
        int safety = 0;

        while (CreatesMatchAtSpawn(x, y, type) && safety < 100)
        {
            type = Random.Range(0, typeCount);
            safety++;
        }

        return type;
    }

    private bool CreatesMatchAtSpawn(int x, int y, int type)
    {
        if (x >= 2)
        {
            Gem left1 = gems[x - 1, y];
            Gem left2 = gems[x - 2, y];
            if (left1 != null && left2 != null && left1.type == type && left2.type == type)
                return true;
        }

        if (y >= 2)
        {
            Gem down1 = gems[x, y - 1];
            Gem down2 = gems[x, y - 2];
            if (down1 != null && down2 != null && down1.type == type && down2.type == type)
                return true;
        }

        return false;
    }

    private void CreateGemAt(int x, int y)
    {
        GameObject obj = Instantiate(gemPrefab, Vector3.zero, Quaternion.identity, transform);

        int type = 0;
        if (gemSprites != null && gemSprites.Length > 0)
            type = Random.Range(0, gemSprites.Length);

        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (gemSprites != null && gemSprites.Length > 0)
                sr.sprite = gemSprites[type];
            sr.color = Color.white;
        }

        Gem gem = obj.GetComponent<Gem>();
        gem.Init(this, x, y, type);

        float offsetX = (width - 1) * 0.5f;
        float offsetY = (height - 1) * 0.5f;

        Vector3 targetPos = new Vector3(x - offsetX, y - offsetY, 0f);
        Vector3 startPos = new Vector3(targetPos.x, targetPos.y + height, 0f);

        gem.transform.localPosition = startPos;
        gem.SetGridPosition(x, y, true, 0.25f);

        gems[x, y] = gem;
        obj.name = $"Gem ({x},{y})";
    }

    private void AdjustCameraAndBoard()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        float cellSize = 1f;

        float boardWorldWidth = (width - 1) * cellSize;
        float boardWorldHeight = (height - 1) * cellSize;

        transform.position = Vector3.zero;

        float halfBoardHeight = boardWorldHeight * 0.5f;
        float targetOrthoSize = halfBoardHeight + boardTopMargin;

        float aspect = (float)Screen.width / Screen.height;
        float halfBoardWidth = boardWorldWidth * 0.5f + boardSideMargin;
        float orthoFromWidth = halfBoardWidth / aspect;

        mainCamera.orthographicSize = Mathf.Max(targetOrthoSize, orthoFromWidth);
    }

    #endregion

    #region UI Helpers

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    private void UpdateGoalUI()
    {
        if (goalText != null)
            goalText.text = $"Goal: {targetScore}";
    }

    private void UpdateMovesUI()
    {
        if (movesText != null)
            movesText.text = $"Moves: {movesLeft}";
    }

    #endregion

    #region Input Handling

    public void OnGemClicked(Gem gem)
    {
        if (isGameOver) return;
        if (isShuffling) return;
        if (isAnimating) return;
        if (gem == null) return;
        // ICE 칸은 클릭(선택) 자체를 막음
        if (IsIce(gem.x, gem.y))
        {
            // (선택) 피드백: 틱 소리/살짝 흔들기
            // PlaySfx(blockedClip);
            return;
        }

        idleTimer = 0f;
        ClearHint();

        if (selectedGem == null)
        {
            selectedGem = gem;
            selectedGem.SetSelected(true);
        }
        else
        {
            if (selectedGem == gem)
            {
                selectedGem.SetSelected(false);
                selectedGem = null;
            }
            else if (IsAdjacent(selectedGem, gem))
            {
                Gem first = selectedGem;
                Gem second = gem;

                selectedGem.SetSelected(false);
                selectedGem = null;

                StartCoroutine(ResolveTurn(first, second));
            }
            else
            {
                selectedGem.SetSelected(false);
                selectedGem = gem;
                selectedGem.SetSelected(true);
            }
        }
    }

    private bool IsAdjacent(Gem a, Gem b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx + dy) == 1;
    }

    #endregion

    #region Score Popup
    private bool IsIce(int x, int y)
    {
        if (obstacles == null) return false;
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        return obstacles[x, y] == ObstacleType.Ice;
    }
    private void SpawnScorePopupAtWorld(int amount, Vector3 worldPos)
    {
        if (scorePopupPrefab == null || uiCanvasRect == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                uiCanvasRect,
                screenPos,
                null,
                out Vector2 canvasPos))
        {
            return;
        }

        Vector2 randomOffset = new Vector2(
            Random.Range(-popupRandomOffsetX, popupRandomOffsetX),
            Random.Range(-popupRandomOffsetY, popupRandomOffsetY)
        );

        Vector2 startPos = canvasPos + popupBaseOffset + randomOffset;

        GameObject popupObj = Instantiate(scorePopupPrefab, uiCanvasRect);
        var rect = popupObj.GetComponent<RectTransform>();
        var text = popupObj.GetComponent<TMP_Text>();

        rect.anchoredPosition = startPos;
        rect.localScale = Vector3.one * popupScaleFrom;

        if (text != null)
        {
            text.text = "+" + amount;
            Color c = text.color;
            float intensity = Mathf.Clamp01(amount / (baseScorePerGem * 4f));
            float extra = intensity * 0.25f;
            c.r = Mathf.Clamp01(c.r + extra);
            c.g = Mathf.Clamp01(c.g + extra);
            c.b = Mathf.Clamp01(c.b + extra);
            c.a = 1f;
            text.color = c;
        }

        popupObj.SetActive(true);

        float moveUpTarget = startPos.y + popupMoveUp;

        var seq = DOTween.Sequence();
        seq.Append(rect.DOScale(popupScaleTo, popupDuration * 0.35f).SetEase(Ease.OutBack));
        seq.Join(rect.DOAnchorPosY(moveUpTarget, popupDuration).SetEase(Ease.OutQuad));

        if (text != null)
        {
            seq.Join(text.DOFade(0f, popupDuration * 0.7f)
                .SetDelay(popupDuration * 0.15f));
        }

        seq.OnComplete(() => Destroy(popupObj));
    }

    #endregion

    #region Swap / Turn Flow (Single Entry Point)

    private void SwapGems(Gem a, Gem b)
    {
        //  ICE 칸이면 스왑 불가 (최종 차단)
        if (IsIce(a.x, a.y) || IsIce(b.x, b.y))
        {
            // 선택 해제/연출만 하고 종료
            return;
        }
        int ax = a.x;
        int ay = a.y;
        int bx = b.x;
        int by = b.y;

        gems[ax, ay] = b;
        gems[bx, by] = a;

        a.x = bx;
        a.y = by;
        b.x = ax;
        b.y = ay;

        a.SetGridPosition(a.x, a.y);
        b.SetGridPosition(b.x, b.y);
    }

    private IEnumerator ResolveTurn(Gem first, Gem second)
    {
        if (isAnimating) yield break;
        isAnimating = true;

        PlaySfx(swapClip);

        // 1) swap
        SwapGems(first, second);
        yield return new WaitForSeconds(swapResolveDelay);

        // 2) resolve special swap / immediate action
        TurnResult turn = new TurnResult();

        // ColorBomb + Stripe는 별도 루틴(연출 포함)
        if (IsColorStripeCombo(first, second))
        {
            Gem colorBomb = first.IsColorBomb ? first : second;
            Gem stripe = (colorBomb == first) ? second : first;

            yield return StartCoroutine(ColorBombStripeComboRoutine(colorBomb, stripe));
            // ColorBombStripeComboRoutine 내부에서 점수/무브/리필까지 처리하므로 여기서는 "턴 종료 정리"만
            turn.didAction = true;
            yield return StartCoroutine(EndTurnCleanupIfNeeded());
            isAnimating = false;
            yield break;
        }

        // 사용자 룰: Stripe는 색 상관없이 스왑 즉시 발동 (단, ColorBomb/Wrapped 혼합은 조합 로직)
        bool anyColor = first.IsColorBomb || second.IsColorBomb;
        bool anyWrapped = first.IsWrappedBomb || second.IsWrappedBomb;
        bool anyStripe = first.IsRowBomb || first.IsColBomb || second.IsRowBomb || second.IsColBomb;

        // (A) Stripe 커스텀 즉발 (ColorBomb 없고, Wrapped도 없을 때만)
        if (!anyColor && anyStripe && !anyWrapped)
        {
            int cleared = ResolveStripeImmediate(first, second);
            if (cleared > 0)
            {
                AddScoreForClear(cleared, comboMultiplier: 1);
                turn.didAction = true;
                yield return StartCoroutine(PostClearRefillAndEnsure());
            }

            // Stripe 스왑은 "매치가 없어도 액션"이므로 되돌리지 않음
            yield return StartCoroutine(EndTurnIfAction(turn));
            isAnimating = false;
            yield break;
        }

        // (B) Wrapped + Normal 즉발(현재 프로젝트 룰 유지)
        if (!anyColor && IsWrappedNormalSwap(first, second))
        {
            Gem wrapped = first.IsWrappedBomb ? first : second;
            int cleared = ActivateWrapped(wrapped);

            if (cleared > 0)
            {
                AddScoreForClear(cleared, comboMultiplier: 1);
                turn.didAction = true;
                yield return StartCoroutine(PostClearRefillAndEnsure());
            }

            yield return StartCoroutine(EndTurnIfAction(turn));
            isAnimating = false;
            yield break;
        }

        // (C) 그 외 스페셜 조합(Stripe+Wrapped / Wrapped+Wrapped / ColorBomb+X 등)
        int specialCleared = ResolveSpecialSwapIfNeeded(first, second);
        if (specialCleared > 0)
        {
            AddScoreForClear(specialCleared, comboMultiplier: 1);
            turn.didAction = true;

            yield return new WaitForSeconds(popDuration);
            yield return StartCoroutine(PostClearRefillAndEnsure());
        }

        // 3) cascades
        int combo = 0;
        while (true)
        {
            int cleared = CheckMatchesAndClear_WithPromotionsSafe();
            if (cleared <= 0) break;

            combo++;
            AddScoreForClear(cleared, comboMultiplier: combo);

            turn.didAction = true;

            yield return new WaitForSeconds(popDuration);
            yield return StartCoroutine(PostClearRefillAndEnsure());
        }

        // 4) no action -> swap back (classic)
        if (!turn.didAction)
        {
            SwapGems(first, second);
            yield return new WaitForSeconds(swapResolveDelay);

            // 무브가 0이면 셔플
            if (!HasAnyPossibleMove())
                yield return ShuffleRoutine(force: true);

            isAnimating = false;
            yield break;
        }

        // 5) action -> end turn once
        ShowComboBanner(combo);
        yield return StartCoroutine(EndTurnIfAction(turn));
        isAnimating = false;
    }

    private struct TurnResult
    {
        public bool didAction;
    }

    private IEnumerator EndTurnIfAction(TurnResult turn)
    {
        if (!turn.didAction) yield break;

        movesLeft--;
        UpdateMovesUI();

        if (score >= targetScore) { EndGame(true); yield break; }
        if (movesLeft <= 0) { EndGame(false); yield break; }

        // 턴 종료 시점에서 보드가 막혔으면 셔플
        if (!HasAnyPossibleMove())
            yield return ShuffleRoutine(force: true);

        yield return StartCoroutine(EndTurnCleanupIfNeeded());
    }

    private IEnumerator EndTurnCleanupIfNeeded()
    {
        ClearHint();
        idleTimer = 0f;
        yield break;
    }

    private void AddScoreForClear(int clearedCount, int comboMultiplier)
    {
        // comboMultiplier: 1부터 시작
        int gained = baseScorePerGem * clearedCount * Mathf.Max(1, comboMultiplier);
        score += gained;
        UpdateScoreUI();
    }

    private IEnumerator PostClearRefillAndEnsure()
    {
        RefillBoard();
        yield return new WaitForSeconds(fallWaitTime);

        // 리필 후 즉시 매치가 생기거나 무브가 0이면 셔플(보드 정합성 확보)
        if (!HasAnyPossibleMove() || HasAnyMatchOnBoard())
            StartCoroutine(ShuffleRoutine(true));
    }

    private bool IsColorStripeCombo(Gem a, Gem b)
    {
        if (a == null || b == null) return false;

        return (a.IsColorBomb && (b.IsRowBomb || b.IsColBomb)) ||
               (b.IsColorBomb && (a.IsRowBomb || a.IsColBomb));
    }

    private bool IsWrappedNormalSwap(Gem a, Gem b)
    {
        if (a == null || b == null) return false;

        bool aWrapped = a.IsWrappedBomb;
        bool bWrapped = b.IsWrappedBomb;

        if (aWrapped && !b.IsSpecial) return true;
        if (bWrapped && !a.IsSpecial) return true;
        return false;
    }

    private int ResolveStripeImmediate(Gem first, Gem second)
    {
        bool firstStripe = first.IsRowBomb || first.IsColBomb;
        bool secondStripe = second.IsRowBomb || second.IsColBomb;

        int cleared = 0;

        if (firstStripe && secondStripe)
        {
            // Stripe+Stripe 조합은 ResolveSpecialSwapIfNeeded에서 처리(3줄/3열/십자)
            cleared = ResolveSpecialSwapIfNeeded(first, second);
        }
        else
        {
            if (firstStripe) cleared += ActivateStripe(first);
            if (secondStripe) cleared += ActivateStripe(second);
        }

        return cleared;
    }

    #endregion

    #region Match / Promotion / Clear (Safe)

    // 매치 스캔 + 승격 + 클리어를 "한 경로"로 처리하되,
    // 승격된 칸은 clearMask에서 제외하여 "매치가 있는데도 제거가 안 되는" 꼬임을 방지한다.
    private int CheckMatchesAndClear_WithPromotionsSafe()
    {
        if (gems == null) return 0;

        bool[,] matched = new bool[width, height];
        ScanRunsToMatched(matched);

        if (!AnyTrue(matched)) return 0;

        bool[,] protect = new bool[width, height];

        // 1) Wrapped 승격 (L/T)
        TryCreateWrappedFromMatches(matched, protect);

        // 2) 4/5런 승격 (Stripe/Color)
        TryCreateStripeOrColorFromRuns(matched, protect);

        // 3) 최종 클리어 마스크 = matched - protect
        bool[,] clearMask = new bool[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                clearMask[x, y] = matched[x, y] && !protect[x, y];
            }
        }

        int cleared = ClearByMaskWithChain(clearMask);
        return cleared;
    }

    private bool AnyTrue(bool[,] mask)
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (mask[x, y]) return true;
        return false;
    }

    private void ScanRunsToMatched(bool[,] matched)
    {
        // 가로
        for (int y = 0; y < height; y++)
        {
            int runType = -1;
            int runStartX = 0;
            int runLength = 0;

            for (int x = 0; x < width; x++)
            {
                int t = (gems[x, y] != null) ? gems[x, y].type : -1;

                if (t == runType && t != -1)
                {
                    runLength++;
                }
                else
                {
                    if (runLength >= 3 && runType != -1)
                    {
                        for (int k = 0; k < runLength; k++)
                            matched[runStartX + k, y] = true;
                    }

                    runType = t;
                    runStartX = x;
                    runLength = (t == -1) ? 0 : 1;
                }
            }

            if (runLength >= 3 && runType != -1)
            {
                for (int k = 0; k < runLength; k++)
                    matched[runStartX + k, y] = true;
            }
        }

        // 세로
        for (int x = 0; x < width; x++)
        {
            int runType = -1;
            int runStartY = 0;
            int runLength = 0;

            for (int y = 0; y < height; y++)
            {
                int t = (gems[x, y] != null) ? gems[x, y].type : -1;

                if (t == runType && t != -1)
                {
                    runLength++;
                }
                else
                {
                    if (runLength >= 3 && runType != -1)
                    {
                        for (int k = 0; k < runLength; k++)
                            matched[x, runStartY + k] = true;
                    }

                    runType = t;
                    runStartY = y;
                    runLength = (t == -1) ? 0 : 1;
                }
            }

            if (runLength >= 3 && runType != -1)
            {
                for (int k = 0; k < runLength; k++)
                    matched[x, runStartY + k] = true;
            }
        }
    }

    private void TryCreateStripeOrColorFromRuns(bool[,] matched, bool[,] protect)
    {
        // 가로 런
        for (int y = 0; y < height; y++)
        {
            int runType = -1;
            int runStartX = 0;
            int runLength = 0;

            for (int x = 0; x <= width; x++)
            {
                int t = -1;
                if (x < width && matched[x, y] && gems[x, y] != null)
                    t = gems[x, y].type;

                if (t == runType && t != -1)
                {
                    runLength++;
                }
                else
                {
                    if (runLength >= 4 && runType != -1)
                    {
                        int specialX = runStartX + runLength / 2;
                        int specialY = y;

                        Gem g = gems[specialX, specialY];
                        if (g != null && g.specialType == SpecialGemType.None)
                        {
                            SpecialGemType st = (runLength >= 5) ? SpecialGemType.ColorBomb : SpecialGemType.RowBomb;
                            g.ForceSetType(g.type, st);
                            protect[specialX, specialY] = true; // 승격된 칸은 제거 금지
                        }
                    }

                    runType = t;
                    runStartX = x;
                    runLength = (t == -1) ? 0 : 1;
                }
            }
        }

        // 세로 런
        for (int x = 0; x < width; x++)
        {
            int runType = -1;
            int runStartY = 0;
            int runLength = 0;

            for (int y = 0; y <= height; y++)
            {
                int t = -1;
                if (y < height && matched[x, y] && gems[x, y] != null)
                    t = gems[x, y].type;

                if (t == runType && t != -1)
                {
                    runLength++;
                }
                else
                {
                    if (runLength >= 4 && runType != -1)
                    {
                        int specialX = x;
                        int specialY = runStartY + runLength / 2;

                        Gem g = gems[specialX, specialY];
                        if (g != null && g.specialType == SpecialGemType.None)
                        {
                            SpecialGemType st = (runLength >= 5) ? SpecialGemType.ColorBomb : SpecialGemType.ColBomb;
                            g.ForceSetType(g.type, st);
                            protect[specialX, specialY] = true; // 승격된 칸은 제거 금지
                        }
                    }

                    runType = t;
                    runStartY = y;
                    runLength = (t == -1) ? 0 : 1;
                }
            }
        }
    }

    private void TryCreateWrappedFromMatches(bool[,] matched, bool[,] protect)
    {
        bool[,] visited = new bool[width, height];

        for (int sx = 0; sx < width; sx++)
        {
            for (int sy = 0; sy < height; sy++)
            {
                if (!matched[sx, sy] || visited[sx, sy])
                    continue;

                List<Vector2Int> cluster = new List<Vector2Int>();
                Queue<Vector2Int> q = new Queue<Vector2Int>();

                q.Enqueue(new Vector2Int(sx, sy));
                visited[sx, sy] = true;

                while (q.Count > 0)
                {
                    var p = q.Dequeue();
                    cluster.Add(p);

                    int x = p.x;
                    int y = p.y;

                    int[] dx4 = { 1, -1, 0, 0 };
                    int[] dy4 = { 0, 0, 1, -1 };

                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + dx4[i];
                        int ny = y + dy4[i];

                        if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                            continue;
                        if (!matched[nx, ny] || visited[nx, ny])
                            continue;

                        visited[nx, ny] = true;
                        q.Enqueue(new Vector2Int(nx, ny));
                    }
                }

                // L/T는 보통 5개 이상(프로젝트 룰 기준)
                if (cluster.Count < 5) continue;

                // 직선 런이면 Wrapped가 아니라 Stripe/Color 승격 대상으로 넘긴다.
                bool allSameX = true;
                bool allSameY = true;

                int baseX = cluster[0].x;
                int baseY = cluster[0].y;

                foreach (var c in cluster)
                {
                    if (c.x != baseX) allSameX = false;
                    if (c.y != baseY) allSameY = false;
                }

                if (allSameX || allSameY) continue;

                Vector2Int pivot = cluster[cluster.Count / 2];
                int px = pivot.x;
                int py = pivot.y;

                Gem g2 = gems[px, py];
                if (g2 != null && g2.specialType == SpecialGemType.None)
                {
                    g2.SetSpecial(SpecialGemType.WrappedBomb);
                    protect[px, py] = true; // Wrapped 승격된 칸은 제거 금지
                }
            }
        }
    }

    private void RefillBoard()
    {
        for (int x = 0; x < width; x++)
        {
            int destY = 0;

            for (int y = 0; y < height; y++)
            {
                if (gems[x, y] != null)
                {
                    if (y != destY)
                    {
                        gems[x, destY] = gems[x, y];
                        gems[x, destY].SetGridPosition(x, destY);
                        gems[x, y] = null;
                    }
                    destY++;
                }
            }

            for (int y = destY; y < height; y++)
                CreateGemAt(x, y);
        }
    }

    #endregion

    #region Combo Banner

    private void ShowComboBanner(int combo)
    {
        if (comboText == null) return;
        if (combo < 2) return;

        string label;
        if (combo >= 6) label = "UNBELIEVABLE!";
        else if (combo >= 5) label = "AMAZING!";
        else if (combo >= 4) label = "AWESOME!";
        else if (combo >= 3) label = "GREAT!";
        else label = "Combo x" + combo;

        comboText.text = label;
        comboText.gameObject.SetActive(true);

        Color c = comboText.color;
        c.a = 1f;
        comboText.color = c;

        RectTransform rect = comboText.rectTransform;
        rect.DOKill();
        comboText.DOKill();

        rect.localScale = Vector3.one * comboScaleFrom;

        var seq = DOTween.Sequence();
        seq.Append(rect.DOScale(comboScaleTo, comboShowTime * 0.35f).SetEase(Ease.OutBack));
        seq.Append(rect.DOScale(1.0f, comboShowTime * 0.25f).SetEase(Ease.OutQuad));
        seq.Join(comboText.DOFade(0f, comboShowTime * 0.6f).SetDelay(comboShowTime * 0.15f));
    }

    private void HideComboBannerImmediate()
    {
        if (comboText == null) return;

        comboText.DOKill();
        var c = comboText.color;
        c.a = 0f;
        comboText.color = c;
        comboText.gameObject.SetActive(false);
    }

    #endregion

    #region Possible Move / Hint

    private bool HasAnyMatchOnBoard()
    {
        if (gems == null) return false;

        // 가로
        for (int y = 0; y < height; y++)
        {
            int x = 0;
            while (x < width)
            {
                if (gems[x, y] == null) { x++; continue; }
                int type = gems[x, y].type;
                int startX = x;
                x++;
                while (x < width && gems[x, y] != null && gems[x, y].type == type) x++;
                if (x - startX >= 3) return true;
            }
        }

        // 세로
        for (int x = 0; x < width; x++)
        {
            int y = 0;
            while (y < height)
            {
                if (gems[x, y] == null) { y++; continue; }
                int type = gems[x, y].type;
                int startY = y;
                y++;
                while (y < height && gems[x, y] != null && gems[x, y].type == type) y++;
                if (y - startY >= 3) return true;
            }
        }

        return false;
    }

    private bool WouldSwapMakeMatch(int x1, int y1, int x2, int y2)
    {
        if (gems == null) return false;
        if (gems[x1, y1] == null || gems[x2, y2] == null) return false;

        Gem g1 = gems[x1, y1];
        Gem g2 = gems[x2, y2];

        int t1 = g1.type;
        int t2 = g2.type;

        g1.type = t2;
        g2.type = t1;

        bool match = HasAnyMatchOnBoard();

        g1.type = t1;
        g2.type = t2;

        return match;
    }

    private bool HasAnyPossibleMove()
    {
        if (gems == null) return false;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Gem g = gems[x, y];
                if (g == null) continue;

                // 특수젬이 있으면 “스왑=액션” 가능성을 높게 본다(현 프로젝트 룰)
                if (g.IsSpecial)
                {
                    if (x < width - 1 && gems[x + 1, y] != null) return true;
                    if (y < height - 1 && gems[x, y + 1] != null) return true;
                }

                if (x < width - 1 && gems[x + 1, y] != null && gems[x + 1, y].IsSpecial) return true;
                if (y < height - 1 && gems[x, y + 1] != null && gems[x, y + 1].IsSpecial) return true;

                if (x < width - 1 && gems[x + 1, y] != null)
                    if (WouldSwapMakeMatch(x, y, x + 1, y)) return true;

                if (y < height - 1 && gems[x, y + 1] != null)
                    if (WouldSwapMakeMatch(x, y, x, y + 1)) return true;
            }
        }

        return false;
    }

    private void ClearHint()
    {
        if (hintGemA != null)
        {
            hintGemA.StopHintEffect();
            hintGemA = null;
        }

        if (hintGemB != null)
        {
            hintGemB.StopHintEffect();
            hintGemB = null;
        }
    }

    private void ShowHintIfPossible()
    {
        if (gems == null) return;
        if (hintGemA != null || hintGemB != null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Gem cur = gems[x, y];
                if (cur == null) continue;

                //  ICE 칸은 힌트 후보 제외 (IsIce가 널-세이프해야 함)
                if (IsIce(x, y)) continue;

                // 오른쪽 이웃
                if (x < width - 1)
                {
                    Gem right = gems[x + 1, y];
                    if (right != null)
                    {
                        //  이웃도 ICE면 제외
                        if (!IsIce(x + 1, y))
                        {
                            if (WouldSwapMakeMatch(x, y, x + 1, y))
                            {
                                hintGemA = cur;
                                hintGemB = right;

                                //  플레이에서만 터지는 NRE 방어 (Destroy 예약/참조 꼬임)
                                if (hintGemA == null || hintGemB == null)
                                {
                                    ClearHint();
                                    return;
                                }

                                hintGemA.PlayHintEffect();
                                hintGemB.PlayHintEffect();
                                return;
                            }
                        }
                    }
                }

                // 위쪽 이웃
                if (y < height - 1)
                {
                    Gem up = gems[x, y + 1];
                    if (up != null)
                    {
                        //  이웃도 ICE면 제외
                        if (!IsIce(x, y + 1))
                        {
                            if (WouldSwapMakeMatch(x, y, x, y + 1))
                            {
                                hintGemA = cur;
                                hintGemB = up;

                                //  플레이에서만 터지는 NRE 방어
                                if (hintGemA == null || hintGemB == null)
                                {
                                    ClearHint();
                                    return;
                                }

                                hintGemA.PlayHintEffect();
                                hintGemB.PlayHintEffect();
                                return;
                            }
                        }
                    }
                }
            }
        }

        ClearHint();
    }


    #endregion

    #region Shuffle

    private void EnsurePlayableBoardImmediate()
    {
        Debug.Log("[EnsurePlayableBoardImmediate] CALLED"); 
        int safety = 0;
        while ((HasAnyMatchOnBoard() || !HasAnyPossibleMove()) && safety < 80)
        {
            ShuffleBoardPreserveSpecial();
            safety++;
        }
    }

    private void ShuffleBoardPreserveSpecial()
    {
        if (gems == null) return;

        List<int> types = new List<int>();
        List<Gem> targets = new List<Gem>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gems[x, y] == null) continue;
                targets.Add(gems[x, y]);
                types.Add(gems[x, y].type);
            }
        }

        for (int i = types.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            int tmp = types[i];
            types[i] = types[r];
            types[r] = tmp;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            Gem g = targets[i];
            int newType = types[i];

            SpecialGemType keep = g.specialType;
            g.ForceSetType(newType, keep);
            g.SnapToGrid();
        }
    }

    private IEnumerator ShuffleRoutine(bool force)
    {
        Debug.Log($"[ShuffleRoutine] ENTER force={force}, isShuffling={isShuffling}, overlayNull={(shuffleOverlay == null)}, textNull={(shuffleText == null)}");

        if (isShuffling)
        {
            Debug.Log("[ShuffleRoutine] EXIT: isShuffling already true");
            yield break;
        }

        if (!force && HasAnyPossibleMove())
        {
            Debug.Log("[ShuffleRoutine] EXIT: has possible move (no shuffle needed)");
            yield break;
        }

        isShuffling = true;

        Debug.Log("[ShuffleRoutine] Turn overlay ON");
        PlaySfx(shuffleClip);
        AlignShuffleTextToBoard();

        if (shuffleOverlay != null) shuffleOverlay.SetActive(true);
        if (shuffleText != null) shuffleText.gameObject.SetActive(true);

        // ✅ 오버레이가 켜지는 프레임을 보장
        yield return null;

        Debug.Log($"[ShuffleRoutine] overlayActive={(shuffleOverlay != null && shuffleOverlay.activeSelf)} textActive={(shuffleText != null && shuffleText.gameObject.activeSelf)}");

        yield return new WaitForSeconds(shuffleMessageTime);

        StartCoroutine(ShuffleRoutine(true));
        yield return new WaitForSeconds(0.25f);

        Debug.Log("[ShuffleRoutine] Turn overlay OFF");
        if (shuffleOverlay != null) shuffleOverlay.SetActive(false);
        if (shuffleText != null) shuffleText.gameObject.SetActive(false);

        ClearHint();
        idleTimer = 0f;

        isShuffling = false;
    }


    private void AlignShuffleTextToBoard()
    {
        if (shuffleText == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position);
        screenPos.y += shuffleTextYOffset;

        shuffleText.rectTransform.position = screenPos;
    }

    #endregion

    #region Game End / Reset / Stage Link

    private void EndGame(bool isWin)
    {
        isGameOver = true;

        bool hasNextStage = (StageManager.Instance != null &&
                             StageManager.Instance.HasNextStage());

        ClearHint();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (resultText != null)
        {
            if (isWin)
            {
                resultText.text = "STAGE CLEAR!";
                resultText.color = new Color(0.3f, 0.9f, 0.4f);
            }
            else
            {
                resultText.text = "FAILED...";
                resultText.color = new Color(0.9f, 0.3f, 0.3f);
            }
        }

        if (clearScoreText != null) clearScoreText.text = $"Score: {score}";
        if (clearGoalText != null) clearGoalText.text = $"Goal: {targetScore}";

        if (clearMovesLeftText != null)
        {
            int usedMoves = maxMoves - movesLeft;
            clearMovesLeftText.text = isWin
                ? $"Remaining Moves: {movesLeft} / {maxMoves}"
                : $"Used Moves: {usedMoves} / {maxMoves}";
        }

        if (isWin)
        {
            int bonusPerMove = 50;
            int bonusScore = movesLeft * bonusPerMove;
            int finalScore = score + bonusScore;

            if (clearBonusText != null) clearBonusText.text = $"Bonus: +{bonusScore}";
            if (clearFinalScoreText != null) clearFinalScoreText.text = $"Final Score: {finalScore}";
        }
        else
        {
            if (clearBonusText != null) clearBonusText.text = "Bonus: +0";
            if (clearFinalScoreText != null) clearFinalScoreText.text = $"Final Score: {score}";
        }

        if (retryButton != null) retryButton.gameObject.SetActive(true);
        if (nextStageButton != null) nextStageButton.gameObject.SetActive(isWin && hasNextStage);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (audioSource == null) return;
        if (clip == null) return;
        audioSource.PlayOneShot(clip);
    }

    public void ResetState()
    {
        isGameOver = false;
        isShuffling = false;
        isAnimating = false;

        selectedGem = null;
        ClearHint();

        score = 0;
        movesLeft = maxMoves;

        UpdateScoreUI();
        UpdateMovesUI();
        HideComboBannerImmediate();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (shuffleOverlay != null) shuffleOverlay.SetActive(false);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (nextStageButton != null) nextStageButton.gameObject.SetActive(false);
    }

    public void RestartGame()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    public void LoadStage(int boardWidth, int boardHeight, int goal, int totalMoves)
    {
        ClearBoard();

        width = boardWidth;
        height = boardHeight;
        targetScore = goal;
        maxMoves = totalMoves;

        isGameOver = false;
        score = 0;
        movesLeft = maxMoves;

        ClearHint();
        HideComboBannerImmediate();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        gems = new Gem[width, height];
        GenerateBoard();
        InitIceArrays();
        ApplyIceForStage4();
        StartCoroutine(ShuffleRoutine(true));

        UpdateScoreUI();
        UpdateGoalUI();
        UpdateMovesUI();

        AdjustCameraAndBoard();

        if (boardPlate != null) boardPlate.gameObject.SetActive(true);
        if (boardGrid != null) boardGrid.gameObject.SetActive(true);

        UpdateBoardPlate();
    }

    public void OnClickNextStage()
    {
        if (StageManager.Instance == null) return;
        StageManager.Instance.GoToNextStage();
    }

    #endregion

    #region Special Combos / Bomb Helpers
    private void InitIceArrays()
    {
        obstacles = new ObstacleType[width, height];
        iceObjects = new GameObject[width, height];
    }
    private Vector3 GridToWorld(int x, int y)
    {
        float ox = (width - 1) * 0.5f;
        float oy = (height - 1) * 0.5f;
        return new Vector3(x - ox, y - oy, 0f);
    }

    private void PlaceIceAt(int x, int y)
    {
        if (icePrefab == null) return;
        if (x < 0 || x >= width || y < 0 || y >= height) return;

        obstacles[x, y] = ObstacleType.Ice;

        GameObject ice = Instantiate(icePrefab, transform);
        ice.transform.localPosition = GridToWorld(x, y);
        iceObjects[x, y] = ice;
    }

    private void BreakAdjacentIceAt(int x, int y)
    {
        TryBreakIce(x + 1, y);
        TryBreakIce(x - 1, y);
        TryBreakIce(x, y + 1);
        TryBreakIce(x, y - 1);
    }

    private void TryBreakIce(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        if (obstacles[x, y] != ObstacleType.Ice) return;

        obstacles[x, y] = ObstacleType.None;
        Destroy(iceObjects[x, y]);
        iceObjects[x, y] = null;
    }

    private void ApplyIceForStage4()
    {
        // StageManager 없어도 동작하게 단순 처리
        int stageNumber = 4; // ← 지금은 고정

        if (stageNumber < 4) return;
        if (stage4IceCells == null) return;

        foreach (var c in stage4IceCells)
            PlaceIceAt(c.x, c.y);
    }

    private int ResolveSpecialSwapIfNeeded(Gem a, Gem b)
    {
        if (a == null || b == null) return 0;

        SpecialGemType sa = a.specialType;
        SpecialGemType sb = b.specialType;

        bool aSpecial = sa != SpecialGemType.None;
        bool bSpecial = sb != SpecialGemType.None;

        if (!aSpecial && !bSpecial) return 0;

        bool[,] mask = new bool[width, height];

        void MarkRow(int row)
        {
            if (row < 0 || row >= height) return;
            for (int x = 0; x < width; x++)
                if (gems[x, row] != null)
                    mask[x, row] = true;
        }

        void MarkCol(int col)
        {
            if (col < 0 || col >= width) return;
            for (int y = 0; y < height; y++)
                if (gems[col, y] != null)
                    mask[col, y] = true;
        }

        void MarkRowRange(int centerRow, int radius)
        {
            for (int dy = -radius; dy <= radius; dy++)
                MarkRow(centerRow + dy);
        }

        void MarkColRange(int centerCol, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
                MarkCol(centerCol + dx);
        }

        // 스왑 중심(Stripe+Stripe 기준점 보정)
        int centerX = (a.x + b.x) / 2;
        int centerY = (a.y + b.y) / 2;

        // ColorBomb + ColorBomb -> 전체
        if (sa == SpecialGemType.ColorBomb && sb == SpecialGemType.ColorBomb)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (gems[x, y] != null)
                        mask[x, y] = true;

            return ClearByMaskWithChain(mask);
        }

        // ColorBomb + Normal -> 해당색 전체
        if (sa == SpecialGemType.ColorBomb && !bSpecial)
        {
            int target = b.type;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (gems[x, y] != null && gems[x, y].type == target)
                        mask[x, y] = true;

            mask[a.x, a.y] = true;
            return ClearByMaskWithChain(mask);
        }
        if (sb == SpecialGemType.ColorBomb && !aSpecial)
        {
            int target = a.type;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (gems[x, y] != null && gems[x, y].type == target)
                        mask[x, y] = true;

            mask[b.x, b.y] = true;
            return ClearByMaskWithChain(mask);
        }

        // Row+Row -> 3줄 (중앙 기준)
        if (sa == SpecialGemType.RowBomb && sb == SpecialGemType.RowBomb)
        {
            MarkRowRange(centerY, 1);
            return ClearByMaskWithChain(mask);
        }

        // Col+Col -> 3열 (중앙 기준)
        if (sa == SpecialGemType.ColBomb && sb == SpecialGemType.ColBomb)
        {
            MarkColRange(centerX, 1);
            return ClearByMaskWithChain(mask);
        }

        // Row+Col -> 십자 (중앙 기준)
        if ((sa == SpecialGemType.RowBomb && sb == SpecialGemType.ColBomb) ||
            (sa == SpecialGemType.ColBomb && sb == SpecialGemType.RowBomb))
        {
            MarkRow(centerY);
            MarkCol(centerX);
            return ClearByMaskWithChain(mask);
        }

        // Stripe + Wrapped -> 3줄+3열(두꺼운 십자), wrapped 위치 기준
        bool aIsStripe = (sa == SpecialGemType.RowBomb || sa == SpecialGemType.ColBomb);
        bool bIsStripe = (sb == SpecialGemType.RowBomb || sb == SpecialGemType.ColBomb);

        if (aIsStripe && sb == SpecialGemType.WrappedBomb)
            return ResolveStripeWrapped(a, b);

        if (bIsStripe && sa == SpecialGemType.WrappedBomb)
            return ResolveStripeWrapped(b, a);

        // Wrapped+Wrapped -> 세로스왑 3x4 / 가로스왑 4x3 (프로젝트 룰)
        if (sa == SpecialGemType.WrappedBomb && sb == SpecialGemType.WrappedBomb)
        {
            bool verticalSwap = (a.x == b.x);
            bool horizontalSwap = (a.y == b.y);

            if (verticalSwap)
            {
                int cx = a.x;
                int minY = Mathf.Min(a.y, b.y) - 1;
                int maxY = Mathf.Max(a.y, b.y) + 1;
                MarkRect(mask, cx - 1, cx + 1, minY, maxY);
            }
            else if (horizontalSwap)
            {
                int cy = a.y;
                int minX = Mathf.Min(a.x, b.x) - 1;
                int maxX = Mathf.Max(a.x, b.x) + 1;
                MarkRect(mask, minX, maxX, cy - 1, cy + 1);
            }
            else
            {
                // 안전 fallback (이론상 안 들어옴)
                Mark3x3(mask, a.x, a.y);
                Mark3x3(mask, b.x, b.y);
            }

            return ClearByMaskWithChain(mask);
        }

        // ColorBomb + Wrapped -> targetType 전체를 3x3로
        if ((sa == SpecialGemType.ColorBomb && sb == SpecialGemType.WrappedBomb) ||
            (sa == SpecialGemType.WrappedBomb && sb == SpecialGemType.ColorBomb))
        {
            int targetType = (sa == SpecialGemType.WrappedBomb) ? a.type : b.type;
            return ResolveColorBombWrapped(targetType);
        }

        return 0;
    }

    private void MarkRect(bool[,] mask, int minX, int maxX, int minY, int maxY)
    {
        minX = Mathf.Max(0, minX);
        maxX = Mathf.Min(width - 1, maxX);
        minY = Mathf.Max(0, minY);
        maxY = Mathf.Min(height - 1, maxY);

        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
                if (gems[x, y] != null) mask[x, y] = true;
    }

    private void Mark3x3(bool[,] mask, int cx, int cy)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            int x = cx + dx;
            if (x < 0 || x >= width) continue;

            for (int dy = -1; dy <= 1; dy++)
            {
                int y = cy + dy;
                if (y < 0 || y >= height) continue;

                if (gems[x, y] != null)
                    mask[x, y] = true;
            }
        }
    }

    private int ResolveStripeWrapped(Gem stripe, Gem wrapped)
    {
        int cx = wrapped.x;
        int cy = wrapped.y;

        bool[,] mask = new bool[width, height];

        // 3줄
        for (int dy = -1; dy <= 1; dy++)
        {
            int y = cy + dy;
            if (y < 0 || y >= height) continue;
            for (int x = 0; x < width; x++)
                if (gems[x, y] != null) mask[x, y] = true;
        }

        // 3열
        for (int dx = -1; dx <= 1; dx++)
        {
            int x = cx + dx;
            if (x < 0 || x >= width) continue;
            for (int y = 0; y < height; y++)
                if (gems[x, y] != null) mask[x, y] = true;
        }

        return ClearByMaskWithChain(mask);
    }

    private int ResolveColorBombWrapped(int targetType)
    {
        if (targetType < 0) return 0;

        bool[,] mask = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Gem g = gems[x, y];
                if (g == null) continue;
                if (g.type != targetType) continue;

                Mark3x3(mask, x, y);
            }
        }

        return ClearByMaskWithChain(mask);
    }

    private int ActivateStripe(Gem bomb)
    {
        if (bomb == null) return 0;

        bool[,] mask = new bool[width, height];

        if (bomb.IsRowBomb)
        {
            int row = bomb.y;
            for (int x = 0; x < width; x++)
                if (gems[x, row] != null) mask[x, row] = true;
        }
        else if (bomb.IsColBomb)
        {
            int col = bomb.x;
            for (int y = 0; y < height; y++)
                if (gems[col, y] != null) mask[col, y] = true;
        }
        else
        {
            return 0;
        }

        return ClearByMaskWithChain(mask);
    }

    private int ActivateWrapped(Gem wrapped)
    {
        if (wrapped == null) return 0;

        bool[,] mask = new bool[width, height];
        Mark3x3(mask, wrapped.x, wrapped.y);

        return ClearByMaskWithChain(mask);
    }

    private IEnumerator ColorBombStripeComboRoutine(Gem colorBomb, Gem stripe)
    {
        if (colorBomb == null || stripe == null)
            yield break;

        int targetType = stripe.type;
        bool isRowStripe = stripe.IsRowBomb;

        List<Gem> candidates = new List<Gem>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Gem g = gems[x, y];
                if (g == null) continue;
                if (g == colorBomb || g == stripe) continue;
                if (g.type != targetType) continue;
                candidates.Add(g);
            }
        }

        if (candidates.Count == 0)
            yield break;

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            Gem tmp = candidates[i];
            candidates[i] = candidates[r];
            candidates[r] = tmp;
        }

        int maxConvert = Mathf.Min(10, candidates.Count);
        List<Gem> converted = new List<Gem>();

        for (int i = 0; i < maxConvert; i++)
        {
            Gem g = candidates[i];
            converted.Add(g);

            Transform t = g.transform;
            t.DOKill();

            Sequence seq = DOTween.Sequence();
            seq.Append(t.DOScale(0.6f, popDuration * 0.25f).SetEase(Ease.InQuad));
            seq.AppendCallback(() =>
            {
                g.SetSpecial(isRowStripe ? SpecialGemType.RowBomb : SpecialGemType.ColBomb);
            });
            seq.Append(t.DOScale(1.1f, popDuration * 0.35f).SetEase(Ease.OutBack));
        }

        yield return new WaitForSeconds(popDuration * 0.7f);

        int totalCleared = 0;

        if (colorBomb != null && gems[colorBomb.x, colorBomb.y] == colorBomb)
        {
            gems[colorBomb.x, colorBomb.y] = null;
            Destroy(colorBomb.gameObject);
            totalCleared++;
        }

        if (stripe != null && gems[stripe.x, stripe.y] == stripe)
        {
            gems[stripe.x, stripe.y] = null;
            Destroy(stripe.gameObject);
            totalCleared++;
        }

        foreach (Gem g in converted)
        {
            if (g == null) continue;
            totalCleared += ActivateStripe(g);
            yield return new WaitForSeconds(0.05f);
        }

        if (totalCleared > 0)
        {
            // 이 콤보는 내부에서 턴 1회로 처리
            AddScoreForClear(totalCleared, comboMultiplier: 1);

            movesLeft--;
            UpdateMovesUI();

            if (score >= targetScore) { EndGame(true); yield break; }
            if (movesLeft <= 0) { EndGame(false); yield break; }

            RefillBoard();
            yield return new WaitForSeconds(fallWaitTime);

            if (!HasAnyPossibleMove())
                yield return ShuffleRoutine(force: true);
        }
    }

    private int ClearByMaskWithChain(bool[,] initialMask)
    {
        if (initialMask == null) return 0;

        bool[,] finalMask = new bool[width, height];
        Queue<Vector2Int> q = new Queue<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!initialMask[x, y]) continue;
                finalMask[x, y] = true;
                q.Enqueue(new Vector2Int(x, y));
            }
        }

        // 체인 확장: Stripe/Wrapped는 확장, ColorBomb은 여기서 확장하지 않음
        while (q.Count > 0)
        {
            Vector2Int p = q.Dequeue();
            int x = p.x;
            int y = p.y;

            Gem g = gems[x, y];
            if (g == null) continue;

            if (!g.IsSpecial) continue;
            if (g.IsColorBomb) continue;

            if (g.IsRowBomb)
            {
                for (int xx = 0; xx < width; xx++)
                {
                    if (!finalMask[xx, y])
                    {
                        finalMask[xx, y] = true;
                        q.Enqueue(new Vector2Int(xx, y));
                    }
                }
            }
            else if (g.IsColBomb)
            {
                for (int yy = 0; yy < height; yy++)
                {
                    if (!finalMask[x, yy])
                    {
                        finalMask[x, yy] = true;
                        q.Enqueue(new Vector2Int(x, yy));
                    }
                }
            }
            else if (g.IsWrappedBomb)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx;
                    if (nx < 0 || nx >= width) continue;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ny = y + dy;
                        if (ny < 0 || ny >= height) continue;

                        if (!finalMask[nx, ny])
                        {
                            finalMask[nx, ny] = true;
                            q.Enqueue(new Vector2Int(nx, ny));
                        }
                    }
                }
            }
        }

        int cleared = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!finalMask[x, y]) continue;
                if (gems[x, y] == null) continue;
                BreakAdjacentIceAt(x, y);
                Gem g = gems[x, y];
                gems[x, y] = null;
                cleared++;

                SpawnScorePopupAtWorld(baseScorePerGem, g.transform.position);
                PlaySfx(matchClip);

                if (matchEffectPrefab != null)
                    Instantiate(matchEffectPrefab, g.transform.position, Quaternion.identity);

                Transform t = g.transform;
                t.DOKill();
                t.localScale = Vector3.one * 0.8f;

                Sequence popSeq = DOTween.Sequence();
                popSeq.Append(t.DOScale(1.25f, popDuration * 0.4f).SetEase(Ease.OutBack))
                      .Append(t.DOScale(0f, popDuration * 0.6f).SetEase(Ease.InBack))
                      .OnComplete(() => Destroy(g.gameObject));
            }
        }

        return cleared;
    }

    #endregion

    #region Debug Helpers (Editor Only)

    private void HandleDebugKeys()
    {
        if (selectedGem == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            selectedGem.ForceSetType(selectedGem.type, SpecialGemType.None);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            selectedGem.SetSpecial(SpecialGemType.RowBomb);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            selectedGem.SetSpecial(SpecialGemType.ColBomb);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            selectedGem.SetSpecial(SpecialGemType.ColorBomb);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            selectedGem.SetSpecial(SpecialGemType.WrappedBomb);
    }

    #endregion
}
