using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 매치3 보드 전체를 관리하는 매니저.
/// - 보드 생성/제거
/// - 입력 처리(젬 클릭, 스왑)
/// - 매치 판정 및 삭제, 중력/리필
/// - 스페셜 젬 조합(스트라이프, 컬러봄, 래핑 등)
/// - 점수, 콤보, 이동 수, 스테이지 클리어/실패 처리
/// - 힌트, 셔플, 카메라 보정, UI 갱신
/// </summary>
public class BoardManager : MonoBehaviour
{
    #region Serialized Fields

    [Header("Camera Settings")]
    public Camera mainCamera;
    [Tooltip("보드 위쪽에 남길 UI 여유 공간")]
    public float boardTopMargin = 2f;
    [Tooltip("보드 좌우에 남길 여유 공간")]
    public float boardSideMargin = 0.5f;

    [Header("Board Plate")]
    public SpriteRenderer boardPlate;     // BoardPlate의 SpriteRenderer
    public SpriteRenderer boardGrid;
    public float boardPlatePadding = 0.5f; // 판이 젬보다 얼마나 더 넓게 나올지 여백
                                           //  색/투명도 컨트롤
    [Header("Board Plate Style")]
    public Color boardPlateColor = new Color(1f, 0.8f, 0.9f, 0.85f); // 파스텔 핑크 + 살짝 투명
    public Color boardGridColor = new Color(1f, 1f, 1f, 0.9f); // 연한 흰색, 알파 낮게

    [Header("Board Size")]
    public int width = 8;
    public int height = 8;

    [Header("Gem Settings")]
    public GameObject gemPrefab;
    public Sprite[] gemSprites;

    [Header("Special Sprites")]
    public Sprite[] wrappedBombSprites; // type별 Wrapped (필요시 사용)

    [Header("Score Settings")]
    public TMP_Text scoreText;
    public int baseScorePerGem = 10;

    [Header("Effect Settings")]
    [Tooltip("젬이 팝될 때 애니메이션 전체 길이")]
    public float popDuration = 0.25f;
    [Tooltip("젬이 떨어지는 연출을 기다릴 시간")]
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
    [Tooltip("스왑 후 매치 판정까지의 딜레이")]
    public float swapResolveDelay = 0.18f;
    [Tooltip("스페셜 폭발 연출 간 짧은 지연 시간")]
    public float specialExplodeDelay = 0.15f;

    [Header("Combo UI")]
    public TMP_Text comboText;
    public float comboShowTime = 0.6f;
    public float comboScaleFrom = 0.7f;
    public float comboScaleTo = 1.2f;

    [Header("Game Rule Settings")]
    public int targetScore = 500;
    [Tooltip("StageManager가 없을 때 사용할 기본 최대 무브 수")]
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
    [Tooltip("입력이 없을 때 힌트를 보여줄까지 대기 시간")]
    public float hintDelay = 5f;

    #endregion

    #region Private Fields

    private Gem[,] gems;                // [x,y] 위치의 젬 배열
    private Gem selectedGem = null;     // 현재 선택된 젬

    private int score = 0;
    private int maxMoves;
    private int movesLeft;

    private bool isGameOver = false;
    private bool isShuffling = false;
    private bool isAnimating = false;

    private float idleTimer = 0f;       // 입력이 없는 시간
    private Gem hintGemA = null;
    private Gem hintGemB = null;

    private int currentComboForPopup = 1;

    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 다른 Awake 내용이 있으면 그 아래/위에 추가
        if (boardPlate != null)
            boardPlate.gameObject.SetActive(false);

        if (boardGrid != null)
            boardGrid.gameObject.SetActive(false);
    }

    private void Start()
    {
        // StageManager가 없으면 여기서 기본 스테이지로 보드를 생성.
        // StageManager가 있으면 StageManager에서 LoadStage를 호출한다고 가정.
        if (StageManager.Instance == null || StageManager.Instance.CurrentStage == null)
        {
            maxMoves = defaultMaxMoves;
            ResetState();

            gems = new Gem[width, height];
            GenerateBoard();

            UpdateScoreUI();
            UpdateGoalUI();
            UpdateMovesUI();
            UpdateBoardPlate();
        }
    }

    private void Update()
    {
        // 디버그 키(F1: 셔플, R: 씬 리로드)
        if (Input.GetKeyDown(KeyCode.F1))
            StartCoroutine(ShuffleRoutine());

        if (Input.GetKeyDown(KeyCode.R))
            RestartGame();

#if UNITY_EDITOR
        HandleDebugKeys();
#endif

        if (isGameOver) return;

        // 애니메이션/셔플/선택중이면 힌트 타이머 정지 + 리셋
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

    /// <summary>
    /// 현재 width, height에 맞춰 보드를 새로 생성한다.
    /// - 초기 생성 시 3매치가 바로 생기지 않도록 타입을 조정한다.
    /// </summary>
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

    /// <summary>
    /// 현재 width / height 기준으로 캔디크러시처럼
    /// 판(BoardPlate) 크기를 맞춘다.
    /// </summary>
    private void UpdateBoardPlate()
    {
        if (boardPlate == null || boardPlate.sprite == null)
            return;

        // 젬 한 칸 = 1유닛 기준 (지금 보드 생성 로직과 동일)
        float cellSize = 1f;

        // 젬이 실제로 깔리는 영역 크기
        float gemsWidth = width * cellSize;
        float gemsHeight = height * cellSize;

        // 판은 그보다 살짝 크게 (padding 양쪽 적용)
        float targetWidth = gemsWidth + boardPlatePadding * 2f;
        float targetHeight = gemsHeight + boardPlatePadding * 2f;

        // 판
        if (boardPlate != null && boardPlate.sprite != null)
        {
            boardPlate.drawMode = SpriteDrawMode.Sliced;
            boardPlate.size = new Vector2(targetWidth, targetHeight);
            boardPlate.transform.localPosition = Vector3.zero;
            boardPlate.color = boardPlateColor;
        }

        // 그리드
        if (boardGrid != null && boardGrid.sprite != null)
        {
            boardGrid.drawMode = SpriteDrawMode.Tiled; // or Sliced
            boardGrid.size = new Vector2(targetWidth, targetHeight);
            boardGrid.transform.localPosition = Vector3.zero;
            boardGrid.color = boardGridColor;
        }
    }


    /// <summary>
    /// 기존 보드의 모든 젬 오브젝트를 제거하고 배열을 초기화한다.
    /// BoardPlate 같은 보드 프레임은 남겨둔다.
    /// </summary>
    private void ClearBoard()
    {
        // 1) 배열에 등록된 젬만 삭제
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

        // 2) 혹시 배열 밖에 있는 젬이 있으면(에러 방지용) 자식 중 Gem만 골라서 삭제
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            // BoardPlate는 건드리지 않음
            if (boardPlate != null && child == boardPlate.transform)
                continue;

            // Gem 컴포넌트가 붙은 애만 삭제
            if (child.GetComponent<Gem>() != null)
            {
                Destroy(child.gameObject);
            }
        }

        gems = null;
    }


    /// <summary>
    /// 초기 보드 생성 시, 같은 타입 3개가 연속되지 않도록 타입을 선택한다.
    /// </summary>
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

    /// <summary>
    /// (x,y)에 주어진 type으로 젬을 놓았을 때
    /// 가로/세로로 3매치가 만들어지는지 검사.
    /// </summary>
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

    /// <summary>
    /// (x,y)에 새 젬을 생성한다.
    /// 위쪽에서 떨어지는 연출을 넣어 사용한다.
    /// </summary>
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

    /// <summary>
    /// 카메라를 보드 크기에 맞게 조정한다.
    /// 세로 기준/가로 기준 중 더 큰 값을 사용해 전체 보드가 화면 안에 들어오게 설정.
    /// </summary>
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

    /// <summary>
    /// Gem.OnMouseDown에서 호출하는 클릭 처리 함수.
    /// - 선택/선택 해제
    /// - 인접 젬이 클릭되면 스왑 코루틴 시작
    /// </summary>
    public void OnGemClicked(Gem gem)
    {
        if (isGameOver) return;
        if (isShuffling) return;
        if (isAnimating) return;
        if (gem == null) return;

        // 입력이 들어왔으므로 힌트 타이머 리셋
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

    /// <summary>
    /// 매치된 젬 위치를 기준으로 캔버스에 점수 팝업 UI를 생성한다.
    /// </summary>
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

    #endregion

    #region Swap / Match / Clear Flow

    /// <summary>
    /// 젬 두 개의 배열 인덱스와 좌표를 교환하고,
    /// SetGridPosition으로 보드 상에서 스왑 연출을 진행한다.
    /// </summary>
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

    /// <summary>
    /// 스왑 후 매치/스페셜 조합, 콤보, 리필, 셔플까지 전체 턴 흐름을 담당하는 코루틴.
    /// </summary>
    private IEnumerator HandleSwapAndMatches(Gem first, Gem second)
    {
        if (isAnimating) yield break;
        isAnimating = true;

        PlaySfx(swapClip);

        // 선택된 두 젬 실제 스왑
        SwapGems(first, second);
        yield return new WaitForSeconds(swapResolveDelay);

        // 1) 스트라이프 전용 커스텀 룰:
        //    Row/Col 스트라이프끼리만 섞였을 때, 색 상관 없이 발동
        bool firstStripe = first.IsRowBomb || first.IsColBomb;
        bool secondStripe = second.IsRowBomb || second.IsColBomb;

        bool anyColorOrWrapped =
            first.IsColorBomb || first.IsWrappedBomb ||
            second.IsColorBomb || second.IsWrappedBomb;

        if ((firstStripe || secondStripe) && !anyColorOrWrapped)
        {
            int cleared = 0;

            if (firstStripe)
                cleared += ActivateStripe(first);

            if (secondStripe)
                cleared += ActivateStripe(second);

            if (cleared > 0)
            {
                score += baseScorePerGem * cleared;
                UpdateScoreUI();

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

                RefillBoard();
                yield return new WaitForSeconds(fallWaitTime);

                if (!HasAnyPossibleMove())
                {
                    yield return ShuffleRoutine();
                }
            }

            isAnimating = false;
            yield break;
        }

        // 2) ColorBomb + Stripe 조합 처리
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

        // 3) 그 외 스페셜 조합 처리
        int totalCleared = 0;
        int combo = 0;

        int specialCleared = ResolveSpecialSwapIfNeeded(first, second);
        if (specialCleared > 0)
        {
            combo = 1;
            totalCleared = specialCleared;

            int gained = baseScorePerGem * specialCleared * combo;
            score += gained;
            UpdateScoreUI();

            yield return new WaitForSeconds(popDuration);
            RefillBoard();
            yield return new WaitForSeconds(fallWaitTime);
        }

        // 4) 자동 매치/콤보 반복 처리
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

        // 5) 아무 것도 안 터졌으면 스왑 되돌리기
        if (totalCleared == 0)
        {
            SwapGems(first, second);
            yield return new WaitForSeconds(swapResolveDelay);
        }
        else
        {
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

            if (!HasAnyPossibleMove())
            {
                StartCoroutine(ShuffleRoutine());
            }
        }

        isAnimating = false;
    }

    /// <summary>
    /// 현재 보드에서 매치를 스캔하고, 매치된 젬을 삭제한다.
    /// 4/5 이상 런이 존재할 경우 스페셜로 승격 후 해당 칸은 삭제 대상에서 제외한다.
    /// </summary>
    private int CheckMatchesAndClear()
    {
        if (gems == null)
            return 0;

        bool[,] matched = new bool[width, height];
        int totalMatched = 0;

        // 가로 검사
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

        // 세로 검사
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
        TryCreateWrappedFromMatches(matched);
        // 4/5 이상 런 검사: 스트라이프/컬러봄 생성
        // 가로 런 기준
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
                            SpecialGemType st =
                                (runLength >= 5)
                                ? SpecialGemType.ColorBomb
                                : SpecialGemType.RowBomb;

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

        // 세로 런 기준
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

        // 실제 삭제 및 팝 연출
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!matched[x, y]) continue;
                if (gems[x, y] == null) continue;

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

    /// <summary>
    /// L / T / + 모양처럼 "꺾인" 매치(5칸 이상)에서 Wrapped를 생성한다.
    /// - matched[x,y] == true 인 칸들을 4방향 BFS로 클러스터링
    /// - 클러스터 크기가 5 이상이고, 완전히 가로/세로로만 일직선이 아닌 경우
    ///   → 클러스터 안에서 하나 골라 Wrapped로 승격
    /// - Wrapped로 승격된 칸은 matched에서 false 로 바꿔서 삭제 대상에서 제외
    ///   (Candy Crush에서 T/L 매치 → Wrapped 생성 규칙 구현)
    /// </summary>
    private void TryCreateWrappedFromMatches(bool[,] matched)
    {
        bool[,] visited = new bool[width, height];

        for (int sx = 0; sx < width; sx++)
        {
            for (int sy = 0; sy < height; sy++)
            {
                if (!matched[sx, sy] || visited[sx, sy])
                    continue;

                // 1) matched true 인 칸들끼리 4방향 연결된 클러스터를 모은다.
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

                // 2) 클러스터 크기가 5 미만이면 L/T가 아니라 그냥 3-match 또는 4-match
                if (cluster.Count < 5)
                    continue;

                // 3) 순수 직선(전부 같은 X 또는 전부 같은 Y)인 경우는 제외
                //    → 이런 건 4/5줄 매치이므로 Stripe / ColorBomb 로직에서 처리.
                bool allSameX = true;
                bool allSameY = true;

                int baseX = cluster[0].x;
                int baseY = cluster[0].y;

                foreach (var c in cluster)
                {
                    if (c.x != baseX) allSameX = false;
                    if (c.y != baseY) allSameY = false;
                }

                if (allSameX || allSameY)
                    continue; // 순수 가로/세로 줄 → Wrapped 생성 대상 아님

                // 4) 여기까지 오면 최소 5칸 이상 + 꺾인 모양(L/T/+)이라고 볼 수 있음.
                //    클러스터 가운데 쯤에 있는 칸을 피벗으로 잡아서 Wrapped 생성.
                Vector2Int pivot = cluster[cluster.Count / 2];
                int px = pivot.x;
                int py = pivot.y;

                Gem g2 = gems[px, py];
                if (g2 != null && g2.specialType == SpecialGemType.None)
                {
                    g2.SetSpecial(SpecialGemType.WrappedBomb);
                    matched[px, py] = false; // Wrapped로 승격된 칸은 삭제하지 않는다.

                    Debug.Log($"Wrapped created at ({px},{py}) from cluster size {cluster.Count}");
                }

                // 한 클러스터당 Wrapped는 하나만 만든다.
            }
        }
    }


    /// <summary>
    /// 빈 칸을 위에서 채우는 중력 처리.
    /// - 아래로 당기고
    /// - 남은 공간은 새 젬을 생성한다.
    /// </summary>
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

    #endregion

    #region Possible Move / Hint

    /// <summary>
    /// 보드 상에 이미 3개 이상 연속된 매치가 존재하는지 검사.
    /// 셔플 조건 확인 등에 사용.
    /// </summary>
    private bool HasAnyMatchOnBoard()
    {
        if (gems == null) return false;

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
                    return true;
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
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 두 칸을 가상의 스왑 후 HasAnyMatchOnBoard로 매치 여부를 검사한다.
    /// 실제로 보드를 건드리지 않고 type만 바꿔서 확인한다.
    /// </summary>
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

    /// <summary>
    /// 스왑해서 매치를 만들 수 있는 이동이 하나라도 있는지 검사.
    /// 없으면 셔플 대상.
    /// </summary>
    private bool HasAnyPossibleMove()
    {
        if (gems == null) return false;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Gem g = gems[x, y];
                if (g == null) continue;

                // ===== 핵심: 특수젬이 하나라도 인접해 있으면
                // 너의 룰/스페셜 조합 규칙상 "스왑 = 액션"이므로 무브가 있다고 본다 =====
                if (g.IsSpecial)
                {
                    // 오른쪽
                    if (x < width - 1 && gems[x + 1, y] != null) return true;
                    // 위쪽
                    if (y < height - 1 && gems[x, y + 1] != null) return true;
                }

                // 인접한 상대가 특수젬이어도 무브
                if (x < width - 1 && gems[x + 1, y] != null && gems[x + 1, y].IsSpecial) return true;
                if (y < height - 1 && gems[x, y + 1] != null && gems[x, y + 1].IsSpecial) return true;

                // ===== 기존: 일반젬끼리는 매치 가능성으로 판단 =====
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
    // ===== 연쇄(체인) 폭발 지원 버전 =====
    // mask로 시작해서, 지워지는 도중 포함된 특수젬이 있으면
    // 그 특수젬의 범위를 추가로 확장한 뒤 최종적으로 한 번에 제거한다.
    private int ClearByMaskWithChain(bool[,] initialMask)
    {
        bool[,] finalMask = new bool[width, height];
        Queue<Vector2Int> q = new Queue<Vector2Int>();

        // 초기 마스크 복사 + 큐 초기화
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!initialMask[x, y]) continue;
                finalMask[x, y] = true;
                q.Enqueue(new Vector2Int(x, y));
            }
        }

        // 1) 확장 단계: 특수젬이면 범위를 추가로 finalMask에 반영
        while (q.Count > 0)
        {
            Vector2Int p = q.Dequeue();
            int x = p.x;
            int y = p.y;

            Gem g = gems[x, y];
            if (g == null) continue;

            // ColorBomb은 “연쇄로 터질 때” 룰이 복잡해질 수 있어서,
            // 여기서는 기본적으로 추가 확장은 하지 않음(필요하면 규칙 확장 가능).
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

        // 2) 제거 단계: finalMask를 실제로 삭제
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

        if (cleared > 0) UpdateScoreUI();
        return cleared;
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

    /// <summary>
    /// 가능한 스왑 중 하나를 찾아 힌트 효과를 재생한다.
    /// </summary>
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

    #endregion

    #region Shuffle

    /// <summary>
    /// 현재 보드의 타입들을 모아 셔플한 뒤,
    /// - 현재 즉시 매치가 없고
    /// - 스왑 가능한 매치가 하나 이상 존재하는 상태
    /// 를 만족할 때까지 반복.
    /// </summary>
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

    /// <summary>
    /// 셔플 UI를 보여주고, 실제 보드 셔플을 수행하는 코루틴.
    /// </summary>
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

    /// <summary>
    /// 셔플 텍스트 위치를 보드 중심에 맞춘다.
    /// </summary>
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

    /// <summary>
    /// 클리어 또는 실패 시 게임 종료 처리.
    /// UI 패널과 결과 텍스트, 보너스 계산까지 담당한다.
    /// </summary>
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

    /// <summary>
    /// 내부 상태를 초기화하고 UI를 갱신한다.
    /// 보드 자체는 건드리지 않는다.
    /// </summary>
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

    /// <summary>
    /// 현재 씬을 다시 로드한다. (디버그용)
    /// </summary>
    public void RestartGame()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    /// <summary>
    /// StageManager에서 호출하는 스테이지 로딩 함수.
    /// - 기존 보드 제거
    /// - 새 보드 크기/목표/무브 적용
    /// - 보드 재생성 및 카메라 보정
    /// </summary>
    public void LoadStage(int boardWidth, int boardHeight, int goal, int totalMoves)
    {
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

        width = boardWidth;
        height = boardHeight;
        targetScore = goal;
        maxMoves = totalMoves;

        isGameOver = false;
        score = 0;
        movesLeft = maxMoves;

        ClearHint();
        HideComboBannerImmediate();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        gems = new Gem[width, height];
        GenerateBoard();

        UpdateScoreUI();
        UpdateGoalUI();
        UpdateMovesUI();

        AdjustCameraAndBoard();

        // 스테이지 시작 시점에만 판을 켜고, 사이즈 맞춤
        if (boardPlate != null)
            boardPlate.gameObject.SetActive(true);

        if (boardGrid != null)
            boardGrid.gameObject.SetActive(true);

        UpdateBoardPlate();
    }

    /// <summary>
    /// 결과 패널에서 다음 스테이지 버튼이 클릭될 때 호출.
    /// </summary>
    public void OnClickNextStage()
    {
        if (StageManager.Instance == null)
            return;

        StageManager.Instance.GoToNextStage();
    }

    #endregion

    #region Special Combos / Bomb Helpers

    /// <summary>
    /// 스페셜 젬끼리 또는 ColorBomb + 일반젬 스왑 시
    /// 캔디 크러시식 폭발 조합을 처리한다.
    /// 처리된 경우 지운 젬 개수를 리턴, 아니면 0.
    /// </summary>
    private int ResolveSpecialSwapIfNeeded(Gem a, Gem b)
    {
        if (a == null || b == null) return 0;

        SpecialGemType sa = a.specialType;
        SpecialGemType sb = b.specialType;

        // ColorBomb + WrappedBomb: 별도 처리
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

        // ColorBomb + ColorBomb → 전체 삭제
        if (sa == SpecialGemType.ColorBomb && sb == SpecialGemType.ColorBomb)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (gems[x, y] != null)
                        mask[x, y] = true;

            return ClearByMaskWithChain(mask);
        }

        // ColorBomb + Normal → 해당 색 전체 삭제
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

        // Row + Row → 중심 기준 3줄
        if (sa == SpecialGemType.RowBomb && sb == SpecialGemType.RowBomb)
        {
            int centerRow = a.y;
            MarkRowRange(centerRow, 1);
            return ClearByMaskWithChain(mask);
        }

        // Col + Col → 중심 기준 3열
        if (sa == SpecialGemType.ColBomb && sb == SpecialGemType.ColBomb)
        {
            int centerCol = a.x;
            MarkColRange(centerCol, 1);
            return ClearByMaskWithChain(mask);
        }

        // Row + Col → 십자 폭발
        if ((sa == SpecialGemType.RowBomb && sb == SpecialGemType.ColBomb) ||
            (sa == SpecialGemType.ColBomb && sb == SpecialGemType.RowBomb))
        {
            int row = a.y;
            int col = b.x;

            MarkRow(row);
            MarkCol(col);

            return ClearByMaskWithChain(mask);
        }

        // Stripe(Row/Col) + WrappedBomb → 별도 처리 함수 사용
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

        // Row + Wrapped → Row 중심 5줄 폭발
        if ((sa == SpecialGemType.RowBomb && sb == SpecialGemType.WrappedBomb) ||
            (sa == SpecialGemType.WrappedBomb && sb == SpecialGemType.RowBomb))
        {
            int centerRow = (sa == SpecialGemType.RowBomb) ? a.y : b.y;
            MarkRowRange(centerRow, 2);
            return ClearByMaskWithChain(mask);
        }

        // Col + Wrapped → Col 중심 5열 폭발
        if ((sa == SpecialGemType.ColBomb && sb == SpecialGemType.WrappedBomb) ||
            (sa == SpecialGemType.WrappedBomb && sb == SpecialGemType.ColBomb))
        {
            int centerCol = (sa == SpecialGemType.ColBomb) ? a.x : b.x;
            MarkColRange(centerCol, 2);
            return ClearByMaskWithChain(mask);
        }

        // Wrapped + Wrapped → 두 위치 중심 3×3 영역 합집합
        if (sa == SpecialGemType.WrappedBomb && sb == SpecialGemType.WrappedBomb)
        {
            Mark3x3(mask, a.x, a.y);
            Mark3x3(mask, b.x, b.y);

            return ClearByMaskWithChain(mask);
        }

        // ColorBomb + Wrapped → 해당 색 젬 일부를 래핑 폭발로 처리
        if ((sa == SpecialGemType.ColorBomb && sb == SpecialGemType.WrappedBomb) ||
            (sa == SpecialGemType.WrappedBomb && sb == SpecialGemType.ColorBomb))
        {
            Gem colorGem = (sa == SpecialGemType.ColorBomb) ? a : b;
            Gem wrappedGem = (sa == SpecialGemType.WrappedBomb) ? a : b;

            int targetType = wrappedGem.type;

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

            int maxPick = Mathf.Min(12, candidates.Count);
            int minPick = Mathf.Min(6, maxPick);
            int pickCount = Random.Range(minPick, maxPick + 1);

            for (int i = 0; i < candidates.Count; i++)
            {
                int r = Random.Range(i, candidates.Count);
                var tmp = candidates[i];
                candidates[i] = candidates[r];
                candidates[r] = tmp;
            }

            for (int i = 0; i < pickCount; i++)
            {
                Gem g = candidates[i];
                Mark3x3(mask, g.x, g.y);
            }

            mask[colorGem.x, colorGem.y] = true;
            mask[wrappedGem.x, wrappedGem.y] = true;

            return ClearByMaskWithChain(mask);
        }

        return 0;
    }

   


    /// <summary>
    /// 중심 (cx,cy)를 포함한 3×3 영역을 mask에 표시한다.
    /// </summary>
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

    /// <summary>
    /// Stripe(Row/Col) + WrappedBomb 조합.
    /// Wrapped 위치를 중심으로 가로/세로 3줄씩 십자 모양으로 제거.
    /// </summary>
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
                mask[x, y] = true;
        }

        // 세로 3줄
        for (int dx = -1; dx <= 1; dx++)
        {
            int x = cx + dx;
            if (x < 0 || x >= width) continue;

            for (int y = 0; y < height; y++)
                mask[x, y] = true;
        }

        int cleared = ClearByMask(mask);
        Debug.Log($"Stripe+Wrapped at ({cx},{cy}), cleared {cleared}");
        return cleared;
    }

    /// <summary>
    /// ColorBomb + Wrapped 조합.
    /// targetType 색상의 모든 젬 주변을 3×3 폭발로 처리한다.
    /// </summary>
    private int ResolveColorBombWrapped(int targetType)
    {
        if (targetType < 0)
            return 0;

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

        int cleared = ClearByMask(mask);
        Debug.Log($"ColorBomb+Wrapped: type {targetType}, cleared {cleared}");
        return cleared;
    }

    /// <summary>
    /// mask[x,y] == true 인 모든 칸을 한 번에 제거하고,
    /// 팝 이펙트/점수 팝업/사운드를 재생한다.
    /// </summary>
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

    /// <summary>
    /// ColorBomb + Stripe 조합 코루틴.
    /// 일정 수의 같은 색 젬을 스트라이프로 변신시키고,
    /// 이후 순차적으로 발동시킨다.
    /// </summary>
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

            seq.Append(
                t.DOScale(0.6f, popDuration * 0.25f)
                 .SetEase(Ease.InQuad)
            );
            seq.AppendCallback(() =>
            {
                g.SetSpecial(isRowStripe ? SpecialGemType.RowBomb
                                         : SpecialGemType.ColBomb);
            });
            seq.Append(
                t.DOScale(1.1f, popDuration * 0.35f)
                 .SetEase(Ease.OutBack)
            );
        }

        yield return new WaitForSeconds(popDuration * 0.7f);

        int totalCleared = 0;

        if (colorBomb != null)
        {
            gems[colorBomb.x, colorBomb.y] = null;
            Destroy(colorBomb.gameObject);
            totalCleared++;
        }

        if (stripe != null)
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
            int gained = baseScorePerGem * totalCleared;
            score += gained;
            UpdateScoreUI();

            movesLeft--;
            UpdateMovesUI();

            if (score >= targetScore)
            {
                EndGame(true);
                yield break;
            }

            if (movesLeft <= 0)
            {
                EndGame(false);
                yield break;
            }

            RefillBoard();
            yield return new WaitForSeconds(fallWaitTime);

            if (!HasAnyPossibleMove())
            {
                yield return ShuffleRoutine();
            }
        }
    }

    /// <summary>
    /// 스트라이프 젬 1개를 발동시켜
    /// RowBomb이면 해당 줄 전체, ColBomb이면 해당 열 전체를 제거한다.
    /// </summary>
    private int ActivateStripe(Gem bomb)
    {
        if (bomb == null) return 0;

        bool[,] mask = new bool[width, height];

        if (bomb.IsRowBomb)
        {
            int row = bomb.y;
            for (int x = 0; x < width; x++)
                if (gems[x, row] != null)
                    mask[x, row] = true;
        }
        else if (bomb.IsColBomb)
        {
            int col = bomb.x;
            for (int y = 0; y < height; y++)
                if (gems[col, y] != null)
                    mask[col, y] = true;
        }
        else
        {
            return 0;
        }

        // ★ 여기서 체인 클리어를 사용해야,
        // 줄에 포함된 Wrapped/Stripe 등이 “연쇄로” 같이 확장되어 터진다.
        return ClearByMaskWithChain(mask);
    }


    #endregion

    #region Debug Helpers (Editor Only)

    /// <summary>
    /// 에디터에서 선택된 젬의 스페셜 타입을 빠르게 바꾸기 위한 디버그 키 처리.
    /// 1~5 키로 Normal, Row, Col, ColorBomb, Wrapped로 변경.
    /// </summary>
    private void HandleDebugKeys()
    {
        if (selectedGem == null)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedGem.ForceSetType(0, SpecialGemType.None);
            Debug.Log("Debug: set Normal (type = 0)");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedGem.SetSpecial(SpecialGemType.RowBomb);
            Debug.Log("Debug: set RowBomb");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            selectedGem.SetSpecial(SpecialGemType.ColBomb);
            Debug.Log("Debug: set ColBomb");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            selectedGem.SetSpecial(SpecialGemType.ColorBomb);
            Debug.Log("Debug: set ColorBomb");
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            selectedGem.SetSpecial(SpecialGemType.WrappedBomb);
            Debug.Log($"Debug: set Wrapped at ({selectedGem.x},{selectedGem.y})");
        }
    }

    #endregion
}
