using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera mainCamera;
    public float boardTopMargin = 2f;   // 위쪽 UI 여유
    public float boardSideMargin = 0.5f; // 좌우 여유

    [Header("Board Size")]
    public int width = 8;
    public int height = 8;

    [Header("Gem Settings")]
    public GameObject gemPrefab;
    public Sprite[] gemSprites;

    [Header("Special Sprites")]
    public Sprite[] wrappedBombSprites; // type별 Wrapped (8종)

    [Header("Score Settings")]
    public TMP_Text scoreText;
    public int baseScorePerGem = 10;
    private int score = 0;

    private Gem[,] gems;                // [x,y] 위치의 젬
    private Gem selectedGem = null;     // 현재 선택된 젬

    [Header("Effect Settings")]
    public float popDuration = 0.25f;   // 팝 애니메이션 전체 길이
    public float fallWaitTime = 0.25f;  // 젬이 떨어지는 연출을 기다릴 시간
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
    private bool isShuffling = false;

    [Header("Animation Settings")]
    public float swapResolveDelay = 0.18f;
    private bool isAnimating = false;

    [Header("Combo UI")]
    public TMP_Text comboText;
    public float comboShowTime = 0.6f;
    public float comboScaleFrom = 0.7f;
    public float comboScaleTo = 1.2f;

    [Header("Game Rule Settings")]
    public int targetScore = 500;

    [Tooltip("StageManager가 없을 때 사용할 기본 최대 무브 수")]
    public int defaultMaxMoves = 20;

    private int maxMoves;
    private int movesLeft;

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

    private bool isGameOver = false;

    [Header("Score Popup (Candy Style)")]
    public GameObject scorePopupPrefab;
    public RectTransform uiCanvasRect;
    [Range(0.3f, 1.5f)]
    public float popupDuration = 0.6f;
    [Range(20f, 120f)]
    public float popupMoveUp = 60f;
    public float popupScaleFrom = 0.6f;
    public float popupScaleTo = 1.05f;
    public Vector2 popupBaseOffset = new Vector2(0f, 20f);
    public float popupRandomOffsetX = 12f;
    public float popupRandomOffsetY = 6f;

    [Header("Hint Settings")]
    public float hintDelay = 5f;
    private float idleTimer = 0f;
    private Gem hintGemA = null;
    private Gem hintGemB = null;

    private int currentComboForPopup = 1;

    private struct PendingSpecial
    {
        public int x;
        public int y;
        public SpecialGemType spType;
        public PendingSpecial(int x, int y, SpecialGemType t)
        {
            this.x = x;
            this.y = y;
            this.spType = t;
        }
    }
    private void AdjustCameraAndBoard()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        // 1칸 = 1유닛, 보드는 (0,0)을 중심으로 깔려 있다고 가정
        float cellSize = 1f;

        float boardWorldWidth = (width - 1) * cellSize;
        float boardWorldHeight = (height - 1) * cellSize;

        // 보드를 화면 가운데에 두고 싶다면
        transform.position = Vector3.zero;

        // 세로 기준: 보드+위쪽 여유가 카메라 높이 안에 들어오게
        float halfBoardHeight = boardWorldHeight * 0.5f;
        float targetOrthoSize = halfBoardHeight + boardTopMargin;

        // 가로 기준도 체크 (세로 모드라 대개 세로가 더 타이트하지만 혹시 몰라서)
        float aspect = (float)Screen.width / Screen.height;
        float halfBoardWidth = boardWorldWidth * 0.5f + boardSideMargin;
        float orthoFromWidth = halfBoardWidth / aspect;

        // 둘 중 더 큰 값을 사용
        mainCamera.orthographicSize = Mathf.Max(targetOrthoSize, orthoFromWidth);
    }


    // --------------------------------
    // UI 갱신 함수들
    // --------------------------------

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

                // 5개 이상이면 일단 Wrapped 하나 생성
                if (cluster.Count >= 5)
                {
                    // 일단 클러스터 가운데 쯤을 선택
                    Vector2Int pivot = cluster[cluster.Count / 2];

                    int px = pivot.x;
                    int py = pivot.y;

                    Gem g = gems[px, py];
                    if (g != null)
                    {
                        g.SetSpecial(SpecialGemType.WrappedBomb);
                        matched[px, py] = false; // 이 칸은 삭제하지 않는다

                        Debug.Log($"Wrapped created at ({px},{py}) from cluster size {cluster.Count}");
                    }

                    // 한 클러스터에서 랩드는 하나만
                }
            }
        }
    }


    // --------------------------------
    // Start
    // --------------------------------

    private void Start()
    {
        // StageManager가 있으면 StageManager.Start()에서 LoadStage를 호출한다고 가정.
        // 여기서는 StageManager가 없을 때만 기본 보드를 생성한다.
        if (StageManager.Instance == null || StageManager.Instance.CurrentStage == null)
        {
            maxMoves = defaultMaxMoves;
            ResetState();

            gems = new Gem[width, height];
            GenerateBoard();

            UpdateScoreUI();
            UpdateGoalUI();
            UpdateMovesUI();
        }

    }

    // --------------------------------
    // 보드 생성 / 제거
    // --------------------------------

    private void GenerateBoard()
    {
        if (gemPrefab == null)
        {
            Debug.LogError("Gem Prefab이 BoardManager에 연결되지 않았습니다.");
            return;
        }

        if (gems == null || gems.GetLength(0) != width || gems.GetLength(1) != height)
            gems = new Gem[width, height];

        float offsetX = (width - 1) * 0.5f;
        float offsetY = (height - 1) * 0.5f;

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
                gem.baseColor = sr != null ? sr.color : Color.white;

                gem.SnapToGrid();

                gems[x, y] = gem;
                obj.name = $"Gem ({x},{y})";
            }
        }
    }

    private void ClearBoard()
    {
        if (gems != null)
        {
            for (int x = 0; x < gems.GetLength(0); x++)
            {
                for (int y = 0; y < gems.GetLength(1); y++)
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
            Destroy(transform.GetChild(i).gameObject);
        }

        gems = null;
    }

    private int GetRandomTypeForInitial(int x, int y)
    {
        int typeCount = (gemSprites != null && gemSprites.Length > 0)
            ? gemSprites.Length
            : 5;

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

            if (left1 != null && left2 != null &&
                left1.type == type && left2.type == type)
            {
                return true;
            }
        }

        if (y >= 2)
        {
            Gem down1 = gems[x, y - 1];
            Gem down2 = gems[x, y - 2];

            if (down1 != null && down2 != null &&
                down1.type == type && down2.type == type)
            {
                return true;
            }
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
        gem.baseColor = sr != null ? sr.color : Color.white;

        float offsetX = (width - 1) * 0.5f;
        float offsetY = (height - 1) * 0.5f;

        Vector3 targetPos = new Vector3(x - offsetX, y - offsetY, 0f);
        Vector3 startPos = new Vector3(targetPos.x, targetPos.y + height, 0f);

        gem.transform.localPosition = startPos;

        gem.SetGridPosition(x, y, true, 0.25f);

        gems[x, y] = gem;
        obj.name = $"Gem ({x},{y})";
    }

    private Color GetColorByType(int type)
    {
        switch (type)
        {
            case 0: return new Color(0.9f, 0.3f, 0.3f);
            case 1: return new Color(0.3f, 0.6f, 0.9f);
            case 2: return new Color(0.3f, 0.8f, 0.4f);
            case 3: return new Color(0.95f, 0.85f, 0.4f);
            case 4: return new Color(0.8f, 0.4f, 0.9f);
            default: return Color.white;
        }
    }

    // --------------------------------
    // 입력 처리
    // --------------------------------

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

    // --------------------------------
    // 점수 팝업
    // --------------------------------

    private void SpawnScorePopupAtWorld(int amount, Vector3 worldPos)
    {
        if (scorePopupPrefab == null || uiCanvasRect == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        Vector2 canvasPos;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                uiCanvasRect,
                screenPos,
                null,
                out canvasPos))
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

        seq.Append(
            rect.DOScale(popupScaleTo, popupDuration * 0.35f)
                .SetEase(Ease.OutBack)
        );
        seq.Join(
            rect.DOAnchorPosY(moveUpTarget, popupDuration)
                .SetEase(Ease.OutQuad)
        );

        if (text != null)
        {
            seq.Join(
                text.DOFade(0f, popupDuration * 0.7f)
                    .SetDelay(popupDuration * 0.15f)
            );
        }

        seq.OnComplete(() =>
        {
            Destroy(popupObj);
        });
    }
    // 스페셜 젬끼리, 또는 컬러밤+일반젬 스왑 시 캔디크러시식 폭발을 처리
    // 처리했으면 지운 젬 개수를 리턴, 아니면 0 리턴
    private int ResolveSpecialSwapIfNeeded(Gem a, Gem b)
    {
        if (a == null || b == null) return 0;

        SpecialGemType sa = a.specialType;
        SpecialGemType sb = b.specialType;

        // 1) ColorBomb + WrappedBomb
        if (sa == SpecialGemType.ColorBomb && sb == SpecialGemType.WrappedBomb)
        {
            return ResolveColorBombWrapped(b.type);
        }
        if (sb == SpecialGemType.ColorBomb && sa == SpecialGemType.WrappedBomb)
        {
            return ResolveColorBombWrapped(a.type);
        }

        bool aSpecial = sa != SpecialGemType.None;
        bool bSpecial = sb != SpecialGemType.None;

        // 일반 + 일반 → 특수 조합 없음
        if (!aSpecial && !bSpecial)
            return 0;

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

        // ============================
        // 0) ColorBomb + ColorBomb → 전체 삭제 (그대로 유지)
        // ============================
        if (sa == SpecialGemType.ColorBomb && sb == SpecialGemType.ColorBomb)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (gems[x, y] != null)
                        mask[x, y] = true;

            return ClearByMask(mask);
        }

        // ============================
        // 1) ColorBomb + Normal → 해당 색 전체 삭제
        // ============================
        if (sa == SpecialGemType.ColorBomb && !bSpecial)
        {
            int target = b.type;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (gems[x, y] != null && gems[x, y].type == target)
                        mask[x, y] = true;

            mask[a.x, a.y] = true;
            return ClearByMask(mask);
        }
        if (sb == SpecialGemType.ColorBomb && !aSpecial)
        {
            int target = a.type;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (gems[x, y] != null && gems[x, y].type == target)
                        mask[x, y] = true;

            mask[b.x, b.y] = true;
            return ClearByMask(mask);
        }

        // ============================
        // 2) Row + Row → 중심 기준 3줄
        // ============================
        if (sa == SpecialGemType.RowBomb && sb == SpecialGemType.RowBomb)
        {
            int centerRow = a.y;
            MarkRowRange(centerRow, 1); // 위/중/아래
            return ClearByMask(mask);
        }

        // 3) Col + Col → 중심 기준 3열
        if (sa == SpecialGemType.ColBomb && sb == SpecialGemType.ColBomb)
        {
            int centerCol = a.x;
            MarkColRange(centerCol, 1); // 좌/중/우
            return ClearByMask(mask);
        }

        // 4) Row + Col → 십자폭발
        if ((sa == SpecialGemType.RowBomb && sb == SpecialGemType.ColBomb) ||
            (sa == SpecialGemType.ColBomb && sb == SpecialGemType.RowBomb))
        {
            int row = a.y;
            int col = b.x;

            MarkRow(row);
            MarkCol(col);

            return ClearByMask(mask);
        }
        // 2) Row/Col Stripe + WrappedBomb
        bool aIsStripe = (sa == SpecialGemType.RowBomb || sa == SpecialGemType.ColBomb);
        bool bIsStripe = (sb == SpecialGemType.RowBomb || sb == SpecialGemType.ColBomb);

        if (aIsStripe && sb == SpecialGemType.WrappedBomb)
        {
            return ResolveStripeWrapped(a, b);
        }
        if (bIsStripe && sa == SpecialGemType.WrappedBomb)
        {
            return ResolveStripeWrapped(b, a);
        }
        // ============================
        // 5) Row + Wrapped / Wrapped + Row
        //    → Stripe 방향 기준 5줄 폭발 (위/중/아래 + 추가 2줄)
        // ============================
        if ((sa == SpecialGemType.RowBomb && sb == SpecialGemType.WrappedBomb) ||
            (sa == SpecialGemType.WrappedBomb && sb == SpecialGemType.RowBomb))
        {
            // RowBomb 쪽 줄을 중심으로 5줄
            int centerRow = (sa == SpecialGemType.RowBomb) ? a.y : b.y;
            MarkRowRange(centerRow, 2); // 총 5줄
            return ClearByMask(mask);
        }

        // 6) Col + Wrapped / Wrapped + Col → 5열 폭발
        if ((sa == SpecialGemType.ColBomb && sb == SpecialGemType.WrappedBomb) ||
            (sa == SpecialGemType.WrappedBomb && sb == SpecialGemType.ColBomb))
        {
            int centerCol = (sa == SpecialGemType.ColBomb) ? a.x : b.x;
            MarkColRange(centerCol, 2); // 총 5열
            return ClearByMask(mask);
        }

        // ============================
        // 7) Wrapped + Wrapped
        //    → 두 위치 중심 3×3 폭발(두 영역 합집합)
        // ============================
        if (sa == SpecialGemType.WrappedBomb && sb == SpecialGemType.WrappedBomb)
        {
            Mark3x3(mask, a.x, a.y);
            Mark3x3(mask, b.x, b.y);

            return ClearByMask(mask);
        }

        // ============================
        // 8) ColorBomb + Wrapped
        //    → 해당 색 젬 중 6~12개 랜덤 선택,
        //       각 위치에서 3×3 폭발
        // ============================
        if ((sa == SpecialGemType.ColorBomb && sb == SpecialGemType.WrappedBomb) ||
            (sa == SpecialGemType.WrappedBomb && sb == SpecialGemType.ColorBomb))
        {
            // Wrapped 아닌 쪽이 ColorBomb, Wrapped 쪽의 색을 타겟으로 사용
            Gem colorGem = (sa == SpecialGemType.ColorBomb) ? a : b;
            Gem wrappedGem = (sa == SpecialGemType.WrappedBomb) ? a : b;

            int targetType = wrappedGem.type;

            // 대상 색 젬 리스트 수집
            List<Gem> candidates = new List<Gem>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Gem g = gems[x, y];
                    if (g == null) continue;
                    if (g.type != targetType) continue;

                    candidates.Add(g);
                }
            }

            if (candidates.Count == 0)
                return 0;

            // 6~12개 사이 (하지만 전체 개수를 넘지 않도록)
            int maxPick = Mathf.Min(12, candidates.Count);
            int minPick = Mathf.Min(6, maxPick);
            int pickCount = Random.Range(minPick, maxPick + 1);

            // 랜덤 셔플
            for (int i = 0; i < candidates.Count; i++)
            {
                int r = Random.Range(i, candidates.Count);
                var tmp = candidates[i];
                candidates[i] = candidates[r];
                candidates[r] = tmp;
            }

            // 앞에서 pickCount개만 사용
            for (int i = 0; i < pickCount; i++)
            {
                Gem g = candidates[i];
                Mark3x3(mask, g.x, g.y);
            }

            // 원래 ColorBomb, Wrapped 도 같이 삭제
            mask[colorGem.x, colorGem.y] = true;
            mask[wrappedGem.x, wrappedGem.y] = true;

            return ClearByMask(mask);
        }

        // 그 외 조합은 특수 발동 없음
        return 0;
    }

    // 중심 (cx,cy)를 포함한 3×3 영역을 mask에 표시
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

                mask[x, y] = true;
            }
        }
    }
    // Stripe(Row/Col) + WrappedBomb 조합
    // 중심은 WrappedBomb가 있는 위치 기준
    private int ResolveStripeWrapped(Gem stripe, Gem wrapped)
    {
        int cx = wrapped.x;
        int cy = wrapped.y;

        bool[,] mask = new bool[width, height];

        // 가로 3줄
        for (int dy = -1; dy <= 1; dy++)
        {
            int y = cy + dy;
            if (y < 0 || y >= height) continue;

            for (int x = 0; x < width; x++)
            {
                mask[x, y] = true;
            }
        }

        // 세로 3줄
        for (int dx = -1; dx <= 1; dx++)
        {
            int x = cx + dx;
            if (x < 0 || x >= width) continue;

            for (int y = 0; y < height; y++)
            {
                mask[x, y] = true;
            }
        }

        int cleared = ClearByMask(mask);
        Debug.Log($"Stripe+Wrapped at ({cx},{cy}), cleared {cleared}");
        return cleared;
    }

    // ColorBomb : targetType 색 전체 삭제
    private int ActivateColorBomb(int targetType, Gem colorBombGem)
    {
        int cleared = 0;

        // 컬러밤 자신도 포함
        if (colorBombGem != null)
        {
            PopGem(colorBombGem);
            cleared++;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Gem g = gems[x, y];
                if (g == null) continue;
                if (g.type != targetType) continue;

                PopGem(g);
                cleared++;
            }
        }

        return cleared;
    }

    // RowBomb : 해당 줄 전체 삭제
    private int ActivateRowBomb(int row)
    {
        int cleared = 0;

        if (row < 0 || row >= height)
            return 0;

        for (int x = 0; x < width; x++)
        {
            Gem g = gems[x, row];
            if (g == null) continue;

            PopGem(g);
            cleared++;
        }

        return cleared;
    }

    // ColBomb : 해당 열 전체 삭제
    private int ActivateColBomb(int col)
    {
        int cleared = 0;

        if (col < 0 || col >= width)
            return 0;

        for (int y = 0; y < height; y++)
        {
            Gem g = gems[col, y];
            if (g == null) continue;

            PopGem(g);
            cleared++;
        }

        return cleared;
    }
    // 중심 줄 기준으로 위/아래 1줄까지 총 3줄 폭발
    private int ActivateTripleRowBomb(int centerRow)
    {
        int cleared = 0;

        for (int dy = -1; dy <= 1; dy++)
        {
            int row = centerRow + dy;
            if (row < 0 || row >= height) continue;

            cleared += ActivateRowBomb(row);
        }

        return cleared;
    }

    // 중심 열 기준으로 좌/우 1열까지 총 3열 폭발
    private int ActivateTripleColBomb(int centerCol)
    {
        int cleared = 0;

        for (int dx = -1; dx <= 1; dx++)
        {
            int col = centerCol + dx;
            if (col < 0 || col >= width) continue;

            cleared += ActivateColBomb(col);
        }

        return cleared;
    }
    // ColorBomb + WrappedBomb 조합
    // targetType = WrappedBomb가 가진 색(type)
    private int ResolveColorBombWrapped(int targetType)
    {
        if (targetType < 0)
            return 0;

        bool[,] mask = new bool[width, height];

        // 해당 색의 모든 젬 주변을 Wrapped 폭발처럼 3×3 마킹
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

        int cleared = ClearByMask(mask);
        Debug.Log($"ColorBomb+Wrapped: type {targetType}, cleared {cleared}");
        return cleared;
    }


    private void PopGem(Gem g)
    {
        int x = g.x;
        int y = g.y;

        // 배열에서 제거
        if (gems[x, y] == g)
            gems[x, y] = null;

        // 이펙트 / 사운드 / 점수 팝업은
        // 필요하면 여기에도 추가할 수 있음 (지금은 최소 버전)

        Destroy(g.gameObject);
    }


    // --------------------------------
    // 스왑 + 매치 처리 (스페셜 스왑 조합은 비활성화)
    // --------------------------------

    private IEnumerator HandleSwapAndMatches(Gem first, Gem second)
    {
        // 이미 다른 애니메이션 중이면 무시
        if (isAnimating) yield break;
        isAnimating = true;

        // 스왑 사운드
        PlaySfx(swapClip);

        // 실제 위치 교환
        SwapGems(first, second);

        // 스왑 애니메이션 대기
        yield return new WaitForSeconds(swapResolveDelay);

        int totalCleared = 0;
        int combo = 0;

        // 1) 먼저 "특수젬 + 일반젬 스왑" 조합 검사
        //    (RowBomb / ColBomb / ColorBomb 발동 등)
        int specialCleared = ResolveSpecialSwapIfNeeded(first, second);

        if (specialCleared > 0)
        {
            // 특수 발동도 1콤보로 간주
            combo = 1;
            totalCleared = specialCleared;

            int gained = baseScorePerGem * specialCleared * combo;
            score += gained;

            UpdateScoreUI();

            // 보드 채우기
            yield return new WaitForSeconds(popDuration);
            RefillBoard();
            yield return new WaitForSeconds(fallWaitTime);
        }

        // 2) 이후에는 기존처럼 자동 매치/콤보 처리
        while (true)
        {
            currentComboForPopup = combo + 1;

            int cleared = CheckMatchesAndClear();
            if (cleared == 0)
                break;

            combo++;
            totalCleared += cleared;

            int gained = baseScorePerGem * cleared * combo;
            score += gained;

            Debug.Log($"Combo {combo}! cleared: {cleared}, gained: {gained}, score: {score}");

            UpdateScoreUI();

            yield return new WaitForSeconds(popDuration);

            RefillBoard();

            yield return new WaitForSeconds(fallWaitTime);
        }

        // 3) 아무 것도 안 터졌으면 스왑 되돌리기
        if (totalCleared == 0)
        {
            SwapGems(first, second);
            yield return new WaitForSeconds(swapResolveDelay);
        }
        else
        {
            // 콤보 텍스트, 무브 차감, 클리어/실패 체크
            ShowComboBanner(combo);

            movesLeft--;
            UpdateMovesUI();

            if (score >= targetScore)
            {
                EndGame(true);
                isAnimating = false;
                yield break;
            }

            if (movesLeft <= 0)
            {
                EndGame(false);
                isAnimating = false;
                yield break;
            }

            // 더 이상 가능한 움직임 없으면 셔플
            if (!HasAnyPossibleMove())
            {
                StartCoroutine(ShuffleRoutine());
            }
        }

        isAnimating = false;
    }



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

    // ========= 매치 검사 + 삭제 =========
    // ========= 매치 검사 + 삭제 =========
    private int CheckMatchesAndClear()
    {
        if (gems == null)
            return 0;

        bool[,] matched = new bool[width, height];
        int totalMatched = 0;

        // ----- 가로 검사 -----
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
                    // 이전 런 마감
                    if (runLength >= 3 && runType != -1)
                    {
                        for (int k = 0; k < runLength; k++)
                        {
                            matched[runStartX + k, y] = true;
                        }
                    }

                    runType = t;
                    runStartX = x;
                    runLength = (t == -1) ? 0 : 1;
                }
            }

            // 줄 끝에서 런 처리
            if (runLength >= 3 && runType != -1)
            {
                for (int k = 0; k < runLength; k++)
                {
                    matched[runStartX + k, y] = true;
                }
            }
        }

        // ----- 세로 검사 -----
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
                        {
                            matched[x, runStartY + k] = true;
                        }
                    }

                    runType = t;
                    runStartY = y;
                    runLength = (t == -1) ? 0 : 1;
                }
            }

            if (runLength >= 3 && runType != -1)
            {
                for (int k = 0; k < runLength; k++)
                {
                    matched[x, runStartY + k] = true;
                }
            }
        }

        // 여기까지 오면 matched[x,y] == true 인 칸들이
        // "이번 턴에 제거 대상"으로 찍힌 상태

        // ======================================================
        // 4개 / 5개 이상 직선 매치 → 스트라이프 / 컬러봄 생성
        // (가로 먼저, 그 다음 세로)
        // ======================================================

        // --- 가로 런 기준 ---
        for (int y = 0; y < height; y++)
        {
            int runType = -1;
            int runStartX = 0;
            int runLength = 0;

            // x == width 일 때 강제 마감용으로 한 칸 더 돈다
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
                            SpecialGemType st =
                                (runLength >= 5)
                                ? SpecialGemType.ColorBomb
                                : SpecialGemType.RowBomb;

                            g.ForceSetType(g.type, st);

                            // 스페셜로 승격된 칸은 제거 대상에서 제외
                            matched[specialX, specialY] = false;
                        }
                    }

                    runType = t;
                    runStartX = x;
                    runLength = (t == -1) ? 0 : 1;
                }
            }
        }

        // --- 세로 런 기준 ---
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
                            SpecialGemType st =
                                (runLength >= 5)
                                ? SpecialGemType.ColorBomb
                                : SpecialGemType.ColBomb;

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

        // ----- matched == true 인 칸 실제 제거 -----
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!matched[x, y])
                    continue;
                if (gems[x, y] == null)
                    continue;

                Gem g = gems[x, y];
                gems[x, y] = null;
                totalMatched++;

                int popupScore = baseScorePerGem;
                SpawnScorePopupAtWorld(popupScore, g.transform.position);
                PlaySfx(matchClip);

                if (matchEffectPrefab != null)
                {
                    Vector3 fxPos = g.transform.position;
                    Instantiate(matchEffectPrefab, fxPos, Quaternion.identity);
                }

                Transform t = g.transform;
                t.DOKill();

                t.localScale = Vector3.one * 0.8f;

                Sequence popSeq = DOTween.Sequence();
                popSeq.Append(
                            t.DOScale(1.25f, popDuration * 0.4f)
                             .SetEase(Ease.OutBack)
                       )
                       .Append(
                            t.DOScale(0f, popDuration * 0.6f)
                             .SetEase(Ease.InBack)
                       )
                       .OnComplete(() =>
                       {
                           Destroy(g.gameObject);
                       });
            }
        }

        Debug.Log($"Matched & cleared: {totalMatched}");
        return totalMatched;
    }





    // --------------------------------
    // 빈 칸 채우기
    // --------------------------------

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
            {
                CreateGemAt(x, y);
            }
        }
    }


    // --------------------------------
    // 콤보 배너
    // --------------------------------

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

        seq.Append(
            rect.DOScale(comboScaleTo, comboShowTime * 0.35f)
                .SetEase(Ease.OutBack)
        );
        seq.Append(
            rect.DOScale(1.0f, comboShowTime * 0.25f)
                .SetEase(Ease.OutQuad)
        );
        seq.Join(
            comboText.DOFade(0f, comboShowTime * 0.6f)
                     .SetDelay(comboShowTime * 0.15f)
        );
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

    // --------------------------------
    // 가능한 매치 여부 / 힌트
    // --------------------------------

    private bool HasAnyMatchOnBoard()
    {
        if (gems == null) return false;

        for (int y = 0; y < height; y++)
        {
            int x = 0;
            while (x < width)
            {
                if (gems[x, y] == null)
                {
                    x++;
                    continue;
                }

                int type = gems[x, y].type;
                int startX = x;
                x++;

                while (x < width &&
                       gems[x, y] != null &&
                       gems[x, y].type == type)
                {
                    x++;
                }

                int runLen = x - startX;
                if (runLen >= 3)
                    return true;
            }
        }

        for (int x = 0; x < width; x++)
        {
            int y = 0;
            while (y < height)
            {
                if (gems[x, y] == null)
                {
                    y++;
                    continue;
                }

                int type = gems[x, y].type;
                int startY = y;
                y++;

                while (y < height &&
                       gems[x, y] != null &&
                       gems[x, y].type == type)
                {
                    y++;
                }

                int runLen = y - startY;
                if (runLen >= 3)
                    return true;
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
                if (gems[x, y] == null) continue;

                if (x < width - 1 && gems[x + 1, y] != null)
                {
                    if (WouldSwapMakeMatch(x, y, x + 1, y))
                        return true;
                }

                if (y < height - 1 && gems[x, y + 1] != null)
                {
                    if (WouldSwapMakeMatch(x, y, x, y + 1))
                        return true;
                }
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
        if (hintGemA != null || hintGemB != null)
            return;

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

    // --------------------------------
    // 셔플
    // --------------------------------

    private void ShuffleBoard()
    {
        if (gems == null) return;

        List<int> types = new List<int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gems[x, y] != null)
                    types.Add(gems[x, y].type);
            }
        }

        int safety = 0;

        do
        {
            for (int i = types.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                int tmp = types[i];
                types[i] = types[r];
                types[r] = tmp;
            }

            int index = 0;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (gems[x, y] == null) continue;

                    int newType = types[index++];
                    gems[x, y].type = newType;

                    var sr = gems[x, y].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        if (gemSprites != null && gemSprites.Length > 0)
                            sr.sprite = gemSprites[newType];

                        sr.color = Color.white;
                        gems[x, y].baseColor = sr.color;
                    }

                    gems[x, y].SnapToGrid();
                }
            }

            safety++;
            if (safety > 100)
            {
                Debug.LogWarning("ShuffleBoard: 조건을 만족하는 셔플을 100번 안에 못 찾음. 현재 상태로 진행.");
                break;
            }

        } while (HasAnyMatchOnBoard() || !HasAnyPossibleMove());
    }

    private IEnumerator ShuffleRoutine()
    {
        if (isShuffling)
            yield break;

        isShuffling = true;

        PlaySfx(shuffleClip);

        AlignShuffleTextToBoard();

        if (shuffleOverlay != null)
            shuffleOverlay.SetActive(true);

        if (shuffleText != null)
            shuffleText.gameObject.SetActive(true);

        yield return new WaitForSeconds(shuffleMessageTime);

        ShuffleBoard();

        yield return new WaitForSeconds(0.4f);

        if (shuffleOverlay != null)
            shuffleOverlay.SetActive(false);

        if (shuffleText != null)
            shuffleText.gameObject.SetActive(false);

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

    // --------------------------------
    // 게임 종료 / 리셋 / 스테이지 연동
    // --------------------------------

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

        if (clearScoreText != null)
            clearScoreText.text = $"Score: {score}";

        if (clearGoalText != null)
            clearGoalText.text = $"Goal: {targetScore}";

        if (clearMovesLeftText != null)
        {
            int usedMoves = maxMoves - movesLeft;
            if (isWin)
            {
                clearMovesLeftText.text = $"Remaining Moves: {movesLeft} / {maxMoves}";
            }
            else
            {
                clearMovesLeftText.text = $"Used Moves: {usedMoves} / {maxMoves}";
            }
        }

        if (isWin)
        {
            int bonusPerMove = 50;
            int bonusScore = movesLeft * bonusPerMove;
            int finalScore = score + bonusScore;

            if (clearBonusText != null)
                clearBonusText.text = $"Bonus: +{bonusScore}";

            if (clearFinalScoreText != null)
                clearFinalScoreText.text = $"Final Score: {finalScore}";
        }
        else
        {
            if (clearBonusText != null)
                clearBonusText.text = "Bonus: +0";

            if (clearFinalScoreText != null)
                clearFinalScoreText.text = $"Final Score: {score}";
        }

        if (retryButton != null)
            retryButton.gameObject.SetActive(true);

        if (nextStageButton != null)
            nextStageButton.gameObject.SetActive(isWin && hasNextStage);
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

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (shuffleOverlay != null)
            shuffleOverlay.SetActive(false);

        if (retryButton != null)
            retryButton.gameObject.SetActive(false);

        if (nextStageButton != null)
            nextStageButton.gameObject.SetActive(false);
    }

    public void RestartGame()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    // StageManager에서 호출하는 스테이지 로딩 함수
    public void LoadStage(int boardWidth, int boardHeight, int goal, int totalMoves)
{
    // 1) 기존 젬들 전부 삭제
    if (gems != null)
    {
        int oldW = gems.GetLength(0);
        int oldH = gems.GetLength(1);

        for (int x = 0; x < oldW; x++)
        {
            for (int y = 0; y < oldH; y++)
            {
                if (gems[x, y] != null)
                {
                    Destroy(gems[x, y].gameObject);
                    gems[x, y] = null;
                }
            }
        }
    }
    ClearBoard();

    // 2) 새 스테이지 값 적용
    width       = boardWidth;
    height      = boardHeight;
    targetScore = goal;
    maxMoves    = totalMoves;

    // 3) 상태 리셋
    isGameOver = false;
    score      = 0;
    movesLeft  = maxMoves;

    ClearHint();
    HideComboBannerImmediate();

    if (gameOverPanel != null)
        gameOverPanel.SetActive(false);

    // 4) 새 보드 배열 만들고 재생성 (★ 여기 딱 한 번만!)
    gems = new Gem[width, height];
    GenerateBoard();

    // 5) UI 갱신
    UpdateScoreUI();
    UpdateGoalUI();
    UpdateMovesUI();

    // 6) 카메라/보드 조정
    AdjustCameraAndBoard();
}


    public void OnClickNextStage()
    {
        if (StageManager.Instance == null)
            return;

        StageManager.Instance.GoToNextStage();
    }

    // --------------------------------
    // Update
    // --------------------------------
    private Gem debugSelectedGem;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            StartCoroutine(ShuffleRoutine());

        if (Input.GetKeyDown(KeyCode.R))
            RestartGame();

#if UNITY_EDITOR
        // 에디터에서만 디버그 키 처리
        HandleDebugKeys();
#endif

        idleTimer += Time.deltaTime;

        if (!isShuffling && selectedGem == null)
        {
            if (idleTimer >= hintDelay)
                ShowHintIfPossible();
        }
    }
    private void HandleDebugKeys()
    {
        // 아무 젬도 선택 안 돼 있으면 바로 종료
        if (selectedGem == null)
            return;

        // 1: 일반
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedGem.SetSpecial(SpecialGemType.None);
            Debug.Log("Debug: set Normal");
        }

        // 2: 가로폭탄
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedGem.SetSpecial(SpecialGemType.RowBomb);
            Debug.Log("Debug: set RowBomb");
        }

        // 3: 세로폭탄
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            selectedGem.SetSpecial(SpecialGemType.ColBomb);
            Debug.Log("Debug: set ColBomb");
        }

        // 4: 컬러봄
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            selectedGem.SetSpecial(SpecialGemType.ColorBomb);
            Debug.Log("Debug: set ColorBomb");
        }

        // 5: Wrapped
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            selectedGem.SetSpecial(SpecialGemType.WrappedBomb);
            Debug.Log($"Debug: set Wrapped at ({selectedGem.x},{selectedGem.y})");
        }
    }


    private void MarkRow(bool[,] mask, int row)
    {
        if (row < 0 || row >= height) return;
        for (int x = 0; x < width; x++)
            mask[x, row] = true;
    }

    private void MarkCol(bool[,] mask, int col)
    {
        if (col < 0 || col >= width) return;
        for (int y = 0; y < height; y++)
            mask[col, y] = true;
    }

    // mask[x,y] == true 인 칸들을 특수 폭발처럼 한 번에 제거
    private int ClearByMask(bool[,] mask)
    {
        int cleared = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!mask[x, y]) continue;
                if (gems[x, y] == null) continue;

                Gem g = gems[x, y];
                gems[x, y] = null;
                cleared++;

                int popupScore = baseScorePerGem;
                SpawnScorePopupAtWorld(popupScore, g.transform.position);
                PlaySfx(matchClip);

                if (matchEffectPrefab != null)
                {
                    Vector3 fxPos = g.transform.position;
                    Instantiate(matchEffectPrefab, fxPos, Quaternion.identity);
                }

                Transform t = g.transform;
                t.DOKill();
                t.localScale = Vector3.one * 0.8f;

                Sequence popSeq = DOTween.Sequence();
                popSeq.Append(
                            t.DOScale(1.25f, popDuration * 0.4f)
                             .SetEase(Ease.OutBack)
                       )
                       .Append(
                            t.DOScale(0f, popDuration * 0.6f)
                             .SetEase(Ease.InBack)
                       )
                       .OnComplete(() =>
                       {
                           Destroy(g.gameObject);
                       });
            }
        }

        if (cleared > 0)
        {
            UpdateScoreUI();
        }

        return cleared;
    }
    private Gem GetGemUnderMouse()
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 pos2D = new Vector2(worldPos.x, worldPos.y);

        RaycastHit2D hit = Physics2D.Raycast(pos2D, Vector2.zero);
        if (hit.collider != null)
        {
            return hit.collider.GetComponent<Gem>();
        }

        return null;
    }


}
