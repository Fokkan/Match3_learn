using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour
{
    [Header("Board Size")]
    public int width = 8;
    public int height = 8;

    [Header("Gem Settings")]
    public GameObject gemPrefab;
    public Sprite[] gemSprites;

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

                    sr.color = GetColorByType(type);
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

            sr.color = GetColorByType(type);
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

        bool aColor = sa == SpecialGemType.ColorBomb;
        bool bColor = sb == SpecialGemType.ColorBomb;

        bool aLine = (sa == SpecialGemType.RowBomb || sa == SpecialGemType.ColBomb);
        bool bLine = (sb == SpecialGemType.RowBomb || sb == SpecialGemType.ColBomb);

        bool aNormal = !a.IsSpecial;
        bool bNormal = !b.IsSpecial;

        // 마스크 준비
        bool[,] mask = new bool[width, height];

        // 도우미: 라인폭탄 젬 하나에 대해 줄/열 마킹
        void MarkLinesForGem(Gem g)
        {
            if (g.specialType == SpecialGemType.RowBomb)
                MarkRow(mask, g.y);
            else if (g.specialType == SpecialGemType.ColBomb)
                MarkCol(mask, g.x);
        }

        // 1) ColorBomb + Normal : 해당 색 전체 삭제
        if ((aColor && bNormal) || (bColor && aNormal))
        {
            Gem colorBomb = aColor ? a : b;
            Gem normalGem = aColor ? b : a;

            int targetType = normalGem.type;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (gems[x, y] != null && gems[x, y].type == targetType)
                        mask[x, y] = true;
                }
            }

            // 컬러밤 자신도 제거
            mask[colorBomb.x, colorBomb.y] = true;

            return ClearByMask(mask);
        }

        // 2) ColorBomb + LineBomb : 같은 색의 젬마다 줄/열 폭발 (다중 라인)
        if ((aColor && bLine) || (bColor && aLine))
        {
            Gem colorBomb = aColor ? a : b;
            Gem lineBomb = aLine ? a : b;

            int targetType = lineBomb.type;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Gem g = gems[x, y];
                    if (g == null) continue;

                    if (g.type == targetType)
                    {
                        if (lineBomb.specialType == SpecialGemType.RowBomb)
                            MarkRow(mask, y);       // 해당 색이 있던 줄 전체
                        else if (lineBomb.specialType == SpecialGemType.ColBomb)
                            MarkCol(mask, x);       // 해당 색이 있던 열 전체
                    }
                }
            }

            // 스왑에 참여한 두 젬도 제거
            mask[colorBomb.x, colorBomb.y] = true;
            mask[lineBomb.x, lineBomb.y] = true;

            return ClearByMask(mask);
        }

        // 3) LineBomb + LineBomb : 십자형 대폭발 (두 젬의 줄/열 모두)
        if (aLine && bLine)
        {
            MarkLinesForGem(a);
            MarkLinesForGem(b);

            // 두 젬 위치는 확실히 포함
            mask[a.x, a.y] = true;
            mask[b.x, b.y] = true;

            return ClearByMask(mask);
        }

        // 4) ColorBomb + ColorBomb : 보드 전체 삭제
        if (aColor && bColor)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (gems[x, y] != null)
                        mask[x, y] = true;
                }
            }

            return ClearByMask(mask);
        }

        // 나머지는 일반 스왑 (조합 없음)
        return 0;
    }


    // --------------------------------
    // 스왑 + 매치 처리 (스페셜 스왑 조합은 비활성화)
    // --------------------------------

    private IEnumerator HandleSwapAndMatches(Gem first, Gem second)
    {
        if (isAnimating) yield break;
        isAnimating = true;

        PlaySfx(swapClip);

        // 실제 스왑
        SwapGems(first, second);

        // 스왑 애니메이션 대기
        yield return new WaitForSeconds(swapResolveDelay);

        int totalCleared = 0;
        int combo = 0;

        // 1) 먼저 스페셜 조합 여부 확인
        int specialCleared = ResolveSpecialSwapIfNeeded(first, second);

        if (specialCleared > 0)
        {
            // 스페셜 조합으로 이미 일부가 터졌다면, 그걸 1콤보로 처리
            combo = 1;
            totalCleared = specialCleared;

            int gained = baseScorePerGem * specialCleared * combo;
            score += gained;

            yield return new WaitForSeconds(popDuration);

            RefillBoard();

            yield return new WaitForSeconds(fallWaitTime);
        }

        // 2) 이후에는 기존처럼 일반 매치/콤보 처리 (체인)
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
            UpdateScoreUI();
            ShowComboBanner(combo);

            // 유효한 스왑 1번당 이동 수 1 감소
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

    // --------------------------------
    // 매치 검사 + 삭제 + 스페셜 생성
    // --------------------------------

    private int CheckMatchesAndClear()
    {
        if (gems == null) return 0;

        bool[,] matched = new bool[width, height];
        int totalMatched = 0;

        List<PendingSpecial> specialsToMake = new List<PendingSpecial>();

        // 가로 검사
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
                {
                    for (int k = startX; k < x; k++)
                        matched[k, y] = true;

                    if (runLen >= 4)
                    {
                        int midX = startX + runLen / 2;
                        SpecialGemType spType =
                            (runLen >= 5) ? SpecialGemType.ColorBomb
                                          : SpecialGemType.RowBomb;

                        specialsToMake.Add(new PendingSpecial(midX, y, spType));
                    }
                }
            }
        }

        // 세로 검사
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
                {
                    for (int k = startY; k < y; k++)
                        matched[x, k] = true;

                    if (runLen >= 4)
                    {
                        int midY = startY + runLen / 2;
                        SpecialGemType spType =
                            (runLen >= 5) ? SpecialGemType.ColorBomb
                                          : SpecialGemType.ColBomb;

                        specialsToMake.Add(new PendingSpecial(x, midY, spType));
                    }
                }
            }
        }

        // 스페셜 승격 적용 (해당 칸은 매치에서 살아남음)
        foreach (var sp in specialsToMake)
        {
            int sx = sp.x;
            int sy = sp.y;
            SpecialGemType st = sp.spType;

            if (sx < 0 || sx >= width || sy < 0 || sy >= height)
                continue;
            if (gems[sx, sy] == null)
                continue;

            matched[sx, sy] = false;
            gems[sx, sy].SetSpecial(st);
        }

        // 스페셜이 매치에 포함된 경우 추가 효과 적용
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!matched[x, y]) continue;
                Gem g = gems[x, y];
                if (g == null) continue;

                if (g.specialType == SpecialGemType.RowBomb)
                {
                    for (int cx = 0; cx < width; cx++)
                    {
                        if (gems[cx, y] != null)
                            matched[cx, y] = true;
                    }
                }
                else if (g.specialType == SpecialGemType.ColBomb)
                {
                    for (int cy = 0; cy < height; cy++)
                    {
                        if (gems[x, cy] != null)
                            matched[x, cy] = true;
                    }
                }
                else if (g.specialType == SpecialGemType.ColorBomb)
                {
                    int targetType = g.type;
                    for (int ix = 0; ix < width; ix++)
                    {
                        for (int iy = 0; iy < height; iy++)
                        {
                            if (gems[ix, iy] != null &&
                                gems[ix, iy].type == targetType)
                            {
                                matched[ix, iy] = true;
                            }
                        }
                    }
                }
            }
        }

        // 실제 제거
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (matched[x, y] && gems[x, y] != null)
                {
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
        }

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

                        sr.color = GetColorByType(newType);
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
        ClearBoard();

        width = boardWidth;
        height = boardHeight;
        targetScore = goal;
        maxMoves = totalMoves;

        ResetState();

        gems = new Gem[width, height];
        GenerateBoard();

        UpdateScoreUI();
        UpdateGoalUI();
        UpdateMovesUI();
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            StartCoroutine(ShuffleRoutine());

        if (Input.GetKeyDown(KeyCode.R))
            RestartGame();

        idleTimer += Time.deltaTime;

        if (!isShuffling && selectedGem == null && !isGameOver)
        {
            if (idleTimer >= hintDelay)
                ShowHintIfPossible();
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

    private int ClearByMask(bool[,] mask)
    {
        if (gems == null) return 0;

        int totalCleared = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!mask[x, y]) continue;
                if (gems[x, y] == null) continue;

                Gem g = gems[x, y];
                gems[x, y] = null;
                totalCleared++;

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

        return totalCleared;
    }

}
