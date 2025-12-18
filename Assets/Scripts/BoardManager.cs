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
    public float hintDelay = 5f;

    #endregion

    #region Private Fields

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

            // (3) 시작 시 보드 검증 + 필요 셔플
            EnsurePlayableBoardImmediate();

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

                StartCoroutine(HandleSwapAndMatches(first, second));
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

    #region Swap / Match / Clear Flow

    private void SwapGems(Gem a, Gem b)
    {
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

    private IEnumerator HandleSwapAndMatches(Gem first, Gem second)
    {
        if (isAnimating) yield break;
        isAnimating = true;

        PlaySfx(swapClip);

        SwapGems(first, second);
        yield return new WaitForSeconds(swapResolveDelay);

        bool firstStripe = first.IsRowBomb || first.IsColBomb;
        bool secondStripe = second.IsRowBomb || second.IsColBomb;

        bool firstWrapped = first.IsWrappedBomb;
        bool secondWrapped = second.IsWrappedBomb;

        bool anyColor =
            first.IsColorBomb || second.IsColorBomb;

        // ================================
        // (1) 스트라이프 커스텀 룰 개선:
        // - Stripe+Stripe는 3줄/3열/십자 조합으로 처리
        // - Stripe+Normal은 해당 Stripe 단독 발동
        // ================================
        if (!anyColor && (firstStripe || secondStripe) && !(firstWrapped || secondWrapped))
        {
            int cleared = 0;

            // Stripe+Stripe 조합
            if (firstStripe && secondStripe)
            {
                cleared = ResolveSpecialSwapIfNeeded(first, second); // Row+Row, Col+Col, Row+Col 처리
            }
            else
            {
                if (firstStripe) cleared += ActivateStripe(first);
                if (secondStripe) cleared += ActivateStripe(second);
            }

            if (cleared > 0)
            {
                score += baseScorePerGem * cleared;
                UpdateScoreUI();

                movesLeft--;
                UpdateMovesUI();

                if (score >= targetScore) { EndGame(true); isAnimating = false; yield break; }
                if (movesLeft <= 0) { EndGame(false); isAnimating = false; yield break; }

                RefillBoard();
                yield return new WaitForSeconds(fallWaitTime);

                if (!HasAnyPossibleMove())
                    yield return ShuffleRoutine(force: true);
            }

            isAnimating = false;
            yield break;
        }

        // ================================
        // ColorBomb + Stripe (이미 정상 동작하던 루틴 유지)
        // ================================
        bool isColorStripeCombo =
            (first.IsColorBomb && (second.IsRowBomb || second.IsColBomb)) ||
            (second.IsColorBomb && (first.IsRowBomb || first.IsColBomb));

        if (isColorStripeCombo)
        {
            Gem colorBomb = first.IsColorBomb ? first : second;
            Gem stripe = (colorBomb == first) ? second : first;

            yield return StartCoroutine(ColorBombStripeComboRoutine(colorBomb, stripe));
            isAnimating = false;
            yield break;
        }

        // ================================
        // (2) Wrapped 단독 스왑 룰:
        // Wrapped + Normal 스왑이면 즉시 3x3 폭발
        // ================================
        bool isWrappedNormalSwap =
            !anyColor &&
            ((firstWrapped && !second.IsSpecial) || (secondWrapped && !first.IsSpecial));

        if (isWrappedNormalSwap)
        {
            Gem wrapped = firstWrapped ? first : second;

            int cleared = ActivateWrapped(wrapped);

            if (cleared > 0)
            {
                score += baseScorePerGem * cleared;
                UpdateScoreUI();

                movesLeft--;
                UpdateMovesUI();

                if (score >= targetScore) { EndGame(true); isAnimating = false; yield break; }
                if (movesLeft <= 0) { EndGame(false); isAnimating = false; yield break; }

                RefillBoard();
                yield return new WaitForSeconds(fallWaitTime);

                if (!HasAnyPossibleMove())
                    yield return ShuffleRoutine(force: true);
            }

            isAnimating = false;
            yield break;
        }

        // ================================
        // (기존) 그 외 스페셜 조합
        // ================================
        int totalCleared = 0;
        int combo = 0;

        int specialCleared = ResolveSpecialSwapIfNeeded(first, second);
        if (specialCleared > 0)
        {
            combo = 1;
            totalCleared = specialCleared;

            score += baseScorePerGem * specialCleared * combo;
            UpdateScoreUI();

            yield return new WaitForSeconds(popDuration);
            RefillBoard();
            yield return new WaitForSeconds(fallWaitTime);
        }

        // 자동 매치/콤보
        while (true)
        {
            currentComboForPopup = combo + 1;

            int cleared = CheckMatchesAndClear();
            if (cleared == 0) break;

            combo++;
            totalCleared += cleared;

            int gained = baseScorePerGem * cleared * combo;
            score += gained;
            UpdateScoreUI();

            yield return new WaitForSeconds(popDuration);
            RefillBoard();
            yield return new WaitForSeconds(fallWaitTime);
        }

        // 아무 것도 안 터졌으면 스왑 되돌리기 + (3)(4) 무브 0이면 셔플
        if (totalCleared == 0)
        {
            SwapGems(first, second);
            yield return new WaitForSeconds(swapResolveDelay);

            if (!HasAnyPossibleMove())
                yield return ShuffleRoutine(force: true);
        }
        else
        {
            ShowComboBanner(combo);

            movesLeft--;
            UpdateMovesUI();

            if (score >= targetScore) { EndGame(true); isAnimating = false; yield break; }
            if (movesLeft <= 0) { EndGame(false); isAnimating = false; yield break; }

            if (!HasAnyPossibleMove())
                StartCoroutine(ShuffleRoutine(force: true));
        }

        isAnimating = false;
    }

    private int CheckMatchesAndClear()
    {
        if (gems == null) return 0;

        bool[,] matched = new bool[width, height];

        // (4) 매치 스캔 -> Wrapped 승격 -> Stripe/Color 승격 -> 체인 클리어(단일 경로)
        ScanRunsToMatched(matched);
        TryCreateWrappedFromMatches(matched);
        TryCreateStripeOrColorFromRuns(matched);

        int cleared = ClearByMaskWithChain(matched);
        return cleared;
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

    private void TryCreateStripeOrColorFromRuns(bool[,] matched)
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
                            matched[specialX, specialY] = false;
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
                            matched[specialX, specialY] = false;
                        }
                    }

                    runType = t;
                    runStartY = y;
                    runLength = (t == -1) ? 0 : 1;
                }
            }
        }
    }

    private void TryCreateWrappedFromMatches(bool[,] matched)
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

                if (cluster.Count < 5) continue;

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
                    matched[px, py] = false;
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

                // 특수젬이 있으면 “스왑=액션”이므로 무브 있다고 판단
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
                if (gems[x, y] == null) continue;

                if (x < width - 1 && gems[x + 1, y] != null)
                {
                    if (WouldSwapMakeMatch(x, y, x + 1, y))
                    {
                        hintGemA = gems[x, y];
                        hintGemB = gems[x + 1, y];
                        hintGemA.PlayHintEffect();
                        hintGemB.PlayHintEffect();
                        return;
                    }
                }

                if (y < height - 1 && gems[x, y + 1] != null)
                {
                    if (WouldSwapMakeMatch(x, y, x, y + 1))
                    {
                        hintGemA = gems[x, y];
                        hintGemB = gems[x, y + 1];
                        hintGemA.PlayHintEffect();
                        hintGemB.PlayHintEffect();
                        return;
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
        // 시작/로드 시: “즉시 매치 존재” OR “가능한 무브 없음”이면 셔플
        int safety = 0;
        while ((HasAnyMatchOnBoard() || !HasAnyPossibleMove()) && safety < 50)
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

            // 특수 유지 + 색만 교체
            SpecialGemType keep = g.specialType;
            g.ForceSetType(newType, keep);
            g.SnapToGrid();
        }
    }

    private IEnumerator ShuffleRoutine(bool force)
    {
        if (isShuffling) yield break;

        // force=false면 “정말 무브가 0일 때”만 셔플
        if (!force && HasAnyPossibleMove())
            yield break;

        isShuffling = true;

        PlaySfx(shuffleClip);
        AlignShuffleTextToBoard();

        if (shuffleOverlay != null) shuffleOverlay.SetActive(true);
        if (shuffleText != null) shuffleText.gameObject.SetActive(true);

        yield return new WaitForSeconds(shuffleMessageTime);

        EnsurePlayableBoardImmediate();
        yield return new WaitForSeconds(0.25f);

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

        // (3) 로드 직후도 보드 검증 + 셔플
        EnsurePlayableBoardImmediate();

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

        // (1) Row+Row -> 3줄
        if (sa == SpecialGemType.RowBomb && sb == SpecialGemType.RowBomb)
        {
            MarkRowRange(a.y, 1);
            return ClearByMaskWithChain(mask);
        }

        // (1) Col+Col -> 3열
        if (sa == SpecialGemType.ColBomb && sb == SpecialGemType.ColBomb)
        {
            MarkColRange(a.x, 1);
            return ClearByMaskWithChain(mask);
        }

        // Row+Col -> 십자
        if ((sa == SpecialGemType.RowBomb && sb == SpecialGemType.ColBomb) ||
            (sa == SpecialGemType.ColBomb && sb == SpecialGemType.RowBomb))
        {
            MarkRow(a.y);
            MarkCol(b.x);
            return ClearByMaskWithChain(mask);
        }

        // Stripe + Wrapped -> 3줄+3열(두꺼운 십자)
        bool aIsStripe = (sa == SpecialGemType.RowBomb || sa == SpecialGemType.ColBomb);
        bool bIsStripe = (sb == SpecialGemType.RowBomb || sb == SpecialGemType.ColBomb);

        if (aIsStripe && sb == SpecialGemType.WrappedBomb)
            return ResolveStripeWrapped(a, b);

        if (bIsStripe && sa == SpecialGemType.WrappedBomb)
            return ResolveStripeWrapped(b, a);

        // Wrapped+Wrapped -> 3x3 합집합 (체인으로 추가효과 확장)
        if (sa == SpecialGemType.WrappedBomb && sb == SpecialGemType.WrappedBomb)
        {
            Mark3x3(mask, a.x, a.y);
            Mark3x3(mask, b.x, b.y);
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

        for (int dy = -1; dy <= 1; dy++)
        {
            int y = cy + dy;
            if (y < 0 || y >= height) continue;
            for (int x = 0; x < width; x++)
                if (gems[x, y] != null) mask[x, y] = true;
        }

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
            score += baseScorePerGem * totalCleared;
            UpdateScoreUI();

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
