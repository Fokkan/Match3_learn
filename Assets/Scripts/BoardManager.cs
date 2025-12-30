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
    public float hintDelay = 3f; // seconds before hint appears
                                 // ===== Fall / Refill Flow (CandyCrush-like) =====
    [Header("Fall/ReFill Flow (CandyCrush-like)")]
    [SerializeField] private float flowSpawnExtraY = 0.8f;
    [SerializeField] private float flowSpawnXJitter = 0.35f;

    [SerializeField] private float flowTimePerCell = 0.06f;
    [SerializeField] private float flowMinTime = 0.12f;
    [SerializeField] private float flowMaxTime = 0.55f;

    [SerializeField] private float flowXSettleRatio = 0.55f;
    [SerializeField] private Ease flowEaseY = Ease.InQuad;
    [SerializeField] private Ease flowEaseX = Ease.OutQuad;

    [SerializeField] private bool flowLandingSquash = true;
    [SerializeField] private float flowSquashScaleY = 0.92f;
    [SerializeField] private float flowSquashTime = 0.06f;
    [SerializeField] private bool enableDiagonalFlow = true;

    // 대각선 후보가 좌/우 둘 다 가능할 때 우선순위
    // -1: 왼쪽 우선, +1: 오른쪽 우선, 0: 랜덤
    [SerializeField] private int diagonalPriority = 0;
    // 낙하/리필 애니메이션 시간(칸 수 기반)
    [SerializeField] private float refillTimePerCell = 0.06f;
    [SerializeField] private float refillMinTime = 0.12f;
    [SerializeField] private float refillMaxTime = 0.55f;



    #endregion

    #region Private Fields
    // ===== ICE Obstacle =====
    public enum ObstacleType { None, Ice }

    [Header("Obstacle - Ice")]
    public GameObject icePrefab;              // ICE 프리팹
    public List<Vector2Int> stage4IceCells;   // 스테이지4 얼음 위치

    private ObstacleType[,] obstacles;
    private GameObject[,] iceObjects;
    private int[,] iceHp; // 크랙 단계용 (예: 1~(iceCrackSprites.Length-1))
    private Vector3 gemBaseScale = Vector3.one;

    private void CacheGemBaseScale()
    {
        if (gemPrefab != null) gemBaseScale = gemPrefab.transform.localScale;
        else gemBaseScale = Vector3.one;
    }

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
    private float lastRefillAnimTime = 0f;

    [Header("Ice Break VFX")]
    public float iceBreakDuration = 0.18f;
    public float icePunchScale = 0.16f;
    public GameObject iceBreakFxPrefab;   // 선택
    public AudioClip iceBreakClip;        // 선택
    public Sprite[] iceCrackSprites;   // 0=멀쩡, 1=크랙1, 2=크랙2(선택)
    public float iceCrackStepDelay = 0.06f;

    [Header("Ice SFX (Split)")]
    public AudioClip iceCrack1Clip;    // 0 -> 1
    public AudioClip iceCrack2Clip;    // 1 -> 2 (완전크랙 프레임)
    public AudioClip iceShatterClip;   // 최종 파괴(파편/사라짐)

    [Header("Ice Flash")]
    public float iceFlashIn = 0.04f;        // 흰색으로 가는 시간
    public float iceFlashOut = 0.08f;       // 원래 색으로 돌아오는 시간
    public float iceFinalCrackHold = 0.06f; // 완전크랙(2) 프레임 유지 시간

    [Header("Camera Shake (Ice Shatter)")]
    public bool useIceCameraShake = true;
    public Transform shakeTarget;           // 비우면 Camera.main.transform 사용
    public float iceShakeDuration = 0.12f;
    public float iceShakeStrength = 0.12f;
    public int iceShakeVibrato = 12;

    [Header("Ice Break Score")]
    public int iceBreakScore = 20;
    public bool showIceBreakScorePopup = true;

    [Header("Ice Sorting")]
    public int iceSortingOrder = 10;          // 젬보다 위에 보이게
    public int iceBreakFxSortingOrder = 30;   // 파편 FX는 더 위에

    [Header("Ice Cluster Limit (Stage >= 4 Random)")]
    public bool limitIceClustering = true;
    public int iceMaxAdjacent = 1;            // 0=절대 붙지 않게, 1=살짝만 붙게
    public bool avoidIce2x2 = true;           // 2x2 얼음 블록 방지
    public int iceMaxClusterSize = 4;         // 연결된 얼음 덩어리 최대 크기

    [Header("Ice Hit Limit")]
    public bool iceLimitToOneHitPerMove = true; // ON이면 "한 턴에 ICE는 1회만 데미지"
    private int[,] iceHitStamp;
    private int iceHitStampId = 1;

    [Header("Ice Spawn (Stage >= 4)")]
    public bool useRandomIce = true;
    public int iceCountStage4 = 6;      // 스테이지4 기본 얼음 개수
    public int iceCountPerStage = 2;    // 스테이지 올라갈 때마다 추가
    public int iceMaxCount = 20;        // 상한
    public bool deterministicByStage = true; // 같은 스테이지면 같은 배치
    public int randomSeedOffset = 12345;
    // ===== StageData → Ice Apply =====

    private struct IceClusterRules
    {
        public bool limitClustering;
        public int maxAdjacent;      // 0이면 인접 금지
        public bool avoid2x2;
        public int maxClusterSize;   // 연결 덩어리 최대
    }

    /// <summary>
    /// StageData에 정의된 iceCage / obstacleCount / obstacleLevel로 ICE를 배치한다.
    /// 우선순위: iceCage(고정) > obstacleCount(랜덤)
    /// </summary>
    private void ApplyIceFromStageData(StageData s)
    {
        // ICE 시스템이 없는 빌드/씬에서도 크래시 나지 않게 방어
        if (s == null) return;

        if (obstacles == null || iceObjects == null || iceHp == null) InitIceArrays();

        // StageData에서 방해요소 사용 안 하면 스킵
        if (!s.useObstacles) return;  //

        // (1) 고정 배치 마스크 우선
        if (s.iceCage != null && s.iceCage.Length == width * height) //
        {
            ApplyIceCageMask_TopLeftOrigin(s.iceCage);
            return;
        }

        // (2) 랜덤 배치(개수 기반)
        if (s.obstacleCount <= 0) return; //

        IceClusterRules rules = GetIceRulesFromLevel(s.obstacleLevel); //

        // 결정적 랜덤(같은 스테이지면 같은 배치) 권장: seed에 stageID 활용
        int seed = s.stageID * 1000 + 12345; //
        PlaceRandomIceWithRules(s.obstacleCount, seed, rules);
    }

    /// <summary>
    /// iceCage 배열을 보드에 매핑해서 ICE 배치.
    /// 기본은 “Top-Left 원점(인간이 읽기 쉬운 방식)”
    /// index 0..width-1 => 최상단 행, 다음이 그 아래 행.
    /// </summary>
    private void ApplyIceCageMask_TopLeftOrigin(int[] mask)
    {
        for (int i = 0; i < mask.Length; i++)
        {
            if (mask[i] == 0) continue;

            int x = i % width;
            int yFromTop = i / width;
            int y = (height - 1) - yFromTop; // Top-Left → (0, height-1)

            // 이미 ICE면 스킵
            if (IsIce(x, y)) continue;

            PlaceIceAt(x, y);
        }
    }

    /// <summary>
    /// obstacleLevel(0~3)을 “뭉침 정도” 룰로 변환.
    /// 레벨이 높을수록 덜 붙게(캔디크러시 느낌으로 정리)
    /// </summary>
    private IceClusterRules GetIceRulesFromLevel(int level)
    {
        // 기본값(중간)
        IceClusterRules r = new IceClusterRules
        {
            limitClustering = true,
            maxAdjacent = 1,
            avoid2x2 = true,
            maxClusterSize = 4
        };

        // level: 0=가장 느슨, 1=약간 느슨, 2=기본, 3=가장 엄격
        switch (level)
        {
            case 0:
                r.maxAdjacent = 2;
                r.avoid2x2 = false;
                r.maxClusterSize = 7;
                break;

            case 1:
                r.maxAdjacent = 2;
                r.avoid2x2 = true;
                r.maxClusterSize = 6;
                break;

            case 2:
                // 기본값 유지
                break;

            default: // 3 이상
                r.maxAdjacent = 0;     // 붙지 않게
                r.avoid2x2 = true;
                r.maxClusterSize = 2;
                break;
        }

        return r;
    }

    /// <summary>
    /// 기존 랜덤 배치 로직에 rules를 적용한 버전.
    /// 네 프로젝트의 “클러스터 제한 함수들”을 재사용하도록 구성.
    /// </summary>
    private void PlaceRandomIceWithRules(int count, int seed, IceClusterRules rules)
    {
        // 후보 수집
        List<Vector2Int> candidates = new List<Vector2Int>(width * height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (IsIce(x, y)) continue;
                if (gems != null && gems[x, y] == null) continue;
                candidates.Add(new Vector2Int(x, y));
            }
        }

        if (candidates.Count == 0) return;

        // 결정적 셔플
        System.Random rng = new System.Random(seed);
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int placed = 0;

        // 1차: 룰 적용
        for (int i = 0; i < candidates.Count && placed < count; i++)
        {
            var c = candidates[i];
            if (!CanPlaceIceAtWithRules(c.x, c.y, rules)) continue;

            PlaceIceAt(c.x, c.y);
            placed++;
        }

        // 2차 보험: 너무 덜 깔리면 2x2만 피하고 채움
        if (placed < count)
        {
            for (int i = 0; i < candidates.Count && placed < count; i++)
            {
                var c = candidates[i];
                if (IsIce(c.x, c.y)) continue;

                if (rules.avoid2x2 && WouldForm2x2Ice(c.x, c.y)) continue;

                PlaceIceAt(c.x, c.y);
                placed++;
            }
        }
    }

    /// <summary>
    /// rules 기반 배치 가능 판정.
    /// 여기서는 네 프로젝트에 이미 들어간 인접/2x2/클러스터 검사 함수를 재사용한다고 가정.
    /// (CountAdjacentIce4 / WouldForm2x2Ice / WouldExceedClusterSize 같은 것)
    /// </summary>
    private bool CanPlaceIceAtWithRules(int x, int y, IceClusterRules rules)
    {
        if (!rules.limitClustering) return true;

        if (rules.maxAdjacent >= 0)
        {
            int adj = CountAdjacentIce4(x, y);
            if (adj > rules.maxAdjacent) return false;
        }

        if (rules.avoid2x2 && WouldForm2x2Ice(x, y)) return false;

        if (rules.maxClusterSize > 0 && WouldExceedClusterSize(x, y, rules.maxClusterSize)) return false;

        return true;
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // 보드판(plate/grid)은 Awake에서 강제로 끄지 않는다.
        // 씬에서 필요하면 인스펙터로 비활성화하고,
        // 게임 시작/LoadStage에서 켜는 흐름으로 관리한다.
    }

    private void Start()
    {
        CacheGemBaseScale();
        if (StageManager.Instance == null || StageManager.Instance.CurrentStage == null)
        {
            maxMoves = defaultMaxMoves;
            ResetState();

            gems = new Gem[width, height];
            InitIceArrays();
            GenerateBoard();
            ApplyIceFromStageData(StageManager.Instance != null ? StageManager.Instance.CurrentStage : null);


            ApplyIceForStage(GetStageNumberSafe());
            // 시작 1회 보드 검증(즉시 매치 제거 + 무브 확보)
            StartCoroutine(ShuffleRoutine(force: false));

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
                obj.transform.localScale = gemBaseScale;
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
        // === ICE 오브젝트도 정리 ===
        if (iceObjects != null)
        {
            int w = iceObjects.GetLength(0);
            int h = iceObjects.GetLength(1);
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (iceObjects[x, y] != null)
                    {
                        Destroy(iceObjects[x, y]);
                        iceObjects[x, y] = null;
                    }
                }
            }
        }

        obstacles = null;
        iceObjects = null;
        iceHp = null;

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

    void CreateGemAt(int x, int y)
    {
        // 기본은 보드 최상단에서 흘러나오게
        CreateGemAt(x, y, height - 1);
    }
    private Vector3 GridToLocalPosition(int x, int y)
    {
        float offsetX = (width - 1) * 0.5f;
        float offsetY = (height - 1) * 0.5f;
        return new Vector3(x - offsetX, y - offsetY, 0f);
    }

    private float CalcFlowDurationByCells(int cells)
    {
        float t = cells * flowTimePerCell;
        return Mathf.Clamp(t, flowMinTime, flowMaxTime);
    }

    // spawnFromY: 이 세그먼트의 최상단 y (ICE 세그먼트 리필을 자연스럽게)
    void CreateGemAt(int x, int y, int spawnFromY)
    {
        GameObject obj = Instantiate(gemPrefab, Vector3.zero, Quaternion.identity, transform);
        obj.transform.localScale = gemBaseScale;


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

        Vector3 targetPos = GridToLocal(x, y);
        Vector3 startPos = GetFlowSpawnLocalPos(x, spawnFromY, targetPos);

        gem.transform.localPosition = startPos;

        // 칸 수 기반 낙하 시간(스폰 라인에서 목표 칸까지)
        int cellDelta = Mathf.Abs(spawnFromY - y) + 1;
        float dur = GetFlowDurationByCells(cellDelta);


        //  낙하 Flow 적용(보드 기준 트윈으로 통일) + lastRefillAnimTime 갱신
        gem.ResetVisual();
        gem.transform.localScale = gemBaseScale;

        // 필요하면 스폰에도 약간의 계단식 딜레이를 줄 수 있음(지금은 0으로 권장)
        float delay = 0f;

        Tween tw = AnimateFlowMove(gem.transform, targetPos, dur);
        tw.SetDelay(delay);

        float landingExtra = GetLandingExtraTime();
        lastRefillAnimTime = Mathf.Max(lastRefillAnimTime, delay + dur + landingExtra);



        gems[x, y] = gem;
        obj.name = $"Gem ({x},{y})";
    }
    private float GetLandingExtraTime()
    {
        if (!flowLandingSquash) return 0f;
        return flowSquashTime * 3f;
    }

    private Tween AnimateFlowMove(Transform t, Vector3 targetLocal, float dur)
    {
        dur = Mathf.Max(dur, flowMinTime);

        Vector3 baseScale = gemBaseScale;

        t.DOKill();

        Tween ty = t.DOLocalMoveY(targetLocal.y, dur).SetEase(flowEaseY);
        Tween tx = t.DOLocalMoveX(targetLocal.x, dur * flowXSettleRatio).SetEase(flowEaseX);

        Sequence s = DOTween.Sequence();
        s.Join(ty);
        s.Join(tx);

        if (flowLandingSquash)
        {
            float st = Mathf.Max(0.01f, flowSquashTime);

            s.Append(t.DOScale(new Vector3(baseScale.x * 1.06f, baseScale.y * flowSquashScaleY, baseScale.z), st).SetEase(Ease.OutQuad));
            s.Append(t.DOScale(new Vector3(baseScale.x * 0.98f, baseScale.y * 1.02f, baseScale.z), st).SetEase(Ease.OutQuad));
            s.Append(t.DOScale(baseScale, st).SetEase(Ease.OutQuad));
        }

        s.OnComplete(() =>
        {
            t.localPosition = targetLocal;
            t.localScale = baseScale;
        });

        return s;
    }




    private Vector3 GridToLocal(int x, int y)
    {
        float offsetX = (width - 1) * 0.5f;
        float offsetY = (height - 1) * 0.5f;
        return new Vector3(x - offsetX, y - offsetY, 0f);
    }

    private float GetFlowDurationByCells(int cellDelta)
    {
        float dur = Mathf.Abs(cellDelta) * flowTimePerCell;
        return Mathf.Clamp(dur, flowMinTime, flowMaxTime);
    }

    // 세그먼트 상단(spawnFromY)에서 흘러나오게 시작 위치 생성
    private Vector3 GetFlowSpawnLocalPos(int x, int spawnFromY, Vector3 targetPos)
    {
        //  판 밖 금지: 스폰 라인을 "세그먼트 최상단 칸"으로 둔다.
        Vector3 spawnLine = GridToLocal(x, spawnFromY);

        //  extraY도 너무 크면 밖처럼 보이니 줄인다(테스트용)
        Vector3 start = spawnLine + Vector3.up * Mathf.Min(flowSpawnExtraY, 0.15f);


        // 살짝 좌우 흔들림
        start.x = targetPos.x + Random.Range(-Mathf.Min(flowSpawnXJitter, 0.10f), Mathf.Min(flowSpawnXJitter, 0.10f));
        return start;
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
    private void PlayIceFlash(SpriteRenderer sr)
    {
        if (sr == null) return;

        // 알파는 유지한 채로 흰색 플래시
        Color original = sr.color;
        Color flash = new Color(1f, 1f, 1f, original.a);

        sr.DOKill(); // 색상 트윈 중복 방지

        Sequence seq = DOTween.Sequence();
        seq.Append(sr.DOColor(flash, iceFlashIn).SetEase(Ease.OutQuad));
        seq.Append(sr.DOColor(original, iceFlashOut).SetEase(Ease.OutQuad));
    }

    private void ShakeCameraForIce()
    {
        if (!useIceCameraShake) return;

        Transform t = shakeTarget;
        if (t == null && Camera.main != null) t = Camera.main.transform;
        if (t == null) return;

        t.DOKill(); // 기존 흔들림 중복 방지
        t.DOShakePosition(iceShakeDuration, iceShakeStrength, iceShakeVibrato, 90f, false, true);
    }

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
        if (x < 0 || x >= width || y < 0 || y >= height) return false;

        // 1) 데이터 기준
        if (obstacles != null && obstacles[x, y] == ObstacleType.Ice) return true;

        // 2) 안전장치: 오브젝트가 남아있으면 Ice로 취급 (데이터 누락/초기화 꼬임 방어)
        if (iceObjects != null && iceObjects[x, y] != null) return true;

        return false;
    }
    private void CleanupOrphanIceObjects()
    {
        if (iceObjects == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (iceObjects[x, y] == null) continue;

                // obstacles가 없거나 Ice로 표시되지 않았는데 오브젝트만 있으면 = 고아
                bool shouldBeIce = (obstacles != null && obstacles[x, y] == ObstacleType.Ice);
                if (!shouldBeIce)
                {
                    Destroy(iceObjects[x, y]);
                    iceObjects[x, y] = null;
                    if (iceHp != null) iceHp[x, y] = 0;
                }
            }
        }
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
        //  이번 "턴(스왑 1회)"에서 ICE는 1회만 데미지
        BeginIceHitWindow();
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

                yield return new WaitForSeconds(popDuration);
                yield return StartCoroutine(PostClearRefillAndEnsure());
            }

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

                yield return new WaitForSeconds(popDuration);
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
    private void AddFlatScore(int amount)
    {
        if (amount <= 0) return;
        score += amount;
        UpdateScoreUI();
    }
    private IEnumerator PostClearRefillAndEnsure()
    {
        // 낙하/리필 애니메이션까지 포함해서 처리
        yield return StartCoroutine(RefillBoardRoutine());

        // 리필로 생긴 자연 매치를 끝까지 정리
        yield return StartCoroutine(ResolveCascadesAfterRefill());
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
                int t = (gems[x, y] != null && !IsIce(x, y)) ? gems[x, y].type : -1;


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
                int t = (gems[x, y] != null && !IsIce(x, y)) ? gems[x, y].type : -1;


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
        CleanupOrphanIceObjects();
        // 1) 먼저 보드 전체에서 "수직 + 대각" 흐름 중력 적용
        ApplyGravityWithDiagonalFlow();

        // 2) 남은 빈칸은 기존과 동일하게 "세그먼트 상단"에서 리필(ICE 아래 구간도 채움)
        FillEmptyCellsBySegments();
    }
    private IEnumerator RefillBoardRoutine()
    {
        CleanupOrphanIceObjects();

        // Phase A: 낙하/밀림 (논리 계산 -> 1회 애니메이션)
        lastRefillAnimTime = 0f;

        var moveMap = ApplyGravityWithDiagonalFlow_CollectMoves();
        AnimateCollectedMoves(moveMap);

        float fallTime = Mathf.Max(lastRefillAnimTime, fallWaitTime);
        yield return new WaitForSeconds(fallTime);

        // Phase B: 스폰(세그먼트 리필)
        lastRefillAnimTime = 0f;
        FillEmptyCellsBySegments();

        float spawnTime = Mathf.Max(lastRefillAnimTime, fallWaitTime * 0.5f);
        yield return new WaitForSeconds(spawnTime);

        // 안전 동기화(애니 끝난 뒤)
        ForceSyncGridTransforms();
    }

    private void ApplyGravityWithDiagonalFlow()
    {
        bool movedAny = true;
        int safety = 0;

        while (movedAny && safety++ < 100)
        {
            movedAny = false;

            // Pass 1) 수직 낙하만 먼저 전부 처리
            bool movedVertical = false;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (IsIce(x, y)) continue;
                    if (gems[x, y] != null) continue;

                    if (TryPullDownFromAbove(x, y))
                    {
                        movedVertical = true;
                    }
                }
            }

            if (movedVertical)
            {
                movedAny = true;
                continue; // 수직으로 한 번이라도 움직였으면, 다시 수직부터 반복
            }

            // Pass 2) 수직이 더 이상 불가능할 때만 대각선 허용
            if (enableDiagonalFlow)
            {
                bool movedDiagonal = false;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (IsIce(x, y)) continue;
                        if (gems[x, y] != null) continue;

                        if (TrySlideDownFromDiagonal(x, y))
                        {
                            movedDiagonal = true;
                        }
                    }
                }

                if (movedDiagonal)
                {
                    movedAny = true;
                }
            }
        }
    }

    private struct FlowMoveOp
    {
        public Gem gem;
        public int fromX, fromY;
        public int toX, toY;
    }

    private Dictionary<Gem, FlowMoveOp> ApplyGravityWithDiagonalFlow_CollectMoves()
    {
        var moveMap = new Dictionary<Gem, FlowMoveOp>(width * height);

        bool movedAny = true;
        int safety = 0;

        while (movedAny && safety++ < 100)
        {
            movedAny = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (IsIce(x, y)) continue;
                    if (gems[x, y] != null) continue;

                    if (TryPullDownFromAbove_Collect(x, y, moveMap))
                    {
                        movedAny = true;
                        continue;
                    }

                    if (enableDiagonalFlow && TrySlideDownFromDiagonal_Collect(x, y, moveMap))
                    {
                        movedAny = true;
                        continue;
                    }
                }
            }
        }

        return moveMap;
    }

    private bool TryPullDownFromAbove_Collect(int x, int emptyY, Dictionary<Gem, FlowMoveOp> moveMap)
    {
        for (int yy = emptyY + 1; yy < height; yy++)
        {
            if (IsIce(x, yy)) return false;

            Gem g = gems[x, yy];
            if (g == null) continue;

            MoveGemLogical(x, yy, x, emptyY, moveMap);
            return true;
        }
        return false;
    }

    private bool TrySlideDownFromDiagonal_Collect(int x, int emptyY, Dictionary<Gem, FlowMoveOp> moveMap)
    {
        int srcX = -1;
        int srcY = -1;

        bool leftOk = (x > 0 && emptyY + 1 < height &&
                       !IsIce(x - 1, emptyY + 1) &&
                       gems[x - 1, emptyY + 1] != null);

        bool rightOk = (x < width - 1 && emptyY + 1 < height &&
                        !IsIce(x + 1, emptyY + 1) &&
                        gems[x + 1, emptyY + 1] != null);

        if (leftOk)
        {
            if (IsBlockedBelow(x - 1, emptyY + 1))
            {
                srcX = x - 1;
                srcY = emptyY + 1;
            }
            else leftOk = false;
        }

        if (rightOk)
        {
            if (IsBlockedBelow(x + 1, emptyY + 1))
            {
                if (srcX == -1)
                {
                    srcX = x + 1;
                    srcY = emptyY + 1;
                }
            }
            else rightOk = false;
        }

        if (!leftOk && !rightOk) return false;

        if (leftOk && rightOk)
        {
            if (diagonalPriority == 0)
            {
                if (Random.value < 0.5f) { srcX = x - 1; srcY = emptyY + 1; }
                else { srcX = x + 1; srcY = emptyY + 1; }
            }
            else if (diagonalPriority < 0)
            {
                srcX = x - 1; srcY = emptyY + 1;
            }
            else
            {
                srcX = x + 1; srcY = emptyY + 1;
            }
        }

        if (srcX < 0) return false;

        MoveGemLogical(srcX, srcY, x, emptyY, moveMap);
        return true;
    }

    private void MoveGemLogical(int fromX, int fromY, int toX, int toY, Dictionary<Gem, FlowMoveOp> moveMap)
    {
        if (fromX < 0 || fromX >= width || fromY < 0 || fromY >= height) return;
        if (toX < 0 || toX >= width || toY < 0 || toY >= height) return;

        if (IsIce(toX, toY)) return;

        Gem g = gems[fromX, fromY];
        if (g == null) return;

        if (gems[toX, toY] != null) return;

        gems[fromX, fromY] = null;
        gems[toX, toY] = g;

        if (moveMap.TryGetValue(g, out var op))
        {
            op.toX = toX;
            op.toY = toY;
            moveMap[g] = op;
        }
        else
        {
            moveMap[g] = new FlowMoveOp
            {
                gem = g,
                fromX = fromX,
                fromY = fromY,
                toX = toX,
                toY = toY
            };
        }

        g.x = toX;
        g.y = toY;
    }
    private void AnimateCollectedMoves(Dictionary<Gem, FlowMoveOp> moveMap)
    {
        if (moveMap == null || moveMap.Count == 0) return;

        foreach (var kv in moveMap)
        {
            FlowMoveOp op = kv.Value;
            Gem g = op.gem;
            if (g == null) continue;

            int cells = Mathf.Abs(op.fromY - op.toY) + Mathf.Abs(op.fromX - op.toX);
            float dur = Mathf.Clamp(cells * refillTimePerCell, refillMinTime, refillMaxTime);
            float durClamped = Mathf.Max(dur, flowMinTime);

            Vector3 target = GridToLocal(op.toX, op.toY);

            g.ResetVisual();
            g.transform.localScale = gemBaseScale;
            g.transform.DOKill();

            float delay = 0.02f * op.toY;

            AnimateFlowMove(g.transform, target, durClamped).SetDelay(delay);

            float extra = GetLandingExtraTime();
            lastRefillAnimTime = Mathf.Max(lastRefillAnimTime, delay + durClamped + extra);
        }
    }

    private bool TryPullDownFromAbove(int x, int emptyY)
    {
        // emptyY 위쪽에서 가장 가까운 젬을 찾되, ICE를 만나면 중단(막힘)
        for (int yy = emptyY + 1; yy < height; yy++)
        {
            if (IsIce(x, yy))
                return false; // ICE가 벽 역할 → 그 위는 못 내려옴

            Gem g = gems[x, yy];
            if (g == null) continue;

            MoveGemFlow(x, yy, x, emptyY);
            return true;
        }
        return false;
    }
    private bool TrySlideDownFromDiagonal(int x, int emptyY)
    {
        int leftX = x - 1;
        int rightX = x + 1;
        int srcY = emptyY + 1;

        if (srcY >= height) return false;

        bool leftOk = (leftX >= 0 &&
                       !IsIce(leftX, srcY) &&
                       gems[leftX, srcY] != null &&
                       IsBlockedBelow(leftX, srcY));

        bool rightOk = (rightX < width &&
                        !IsIce(rightX, srcY) &&
                        gems[rightX, srcY] != null &&
                        IsBlockedBelow(rightX, srcY));

        if (!leftOk && !rightOk) return false;

        int chosenX;

        if (leftOk && rightOk)
        {
            // 1) 같은 높이의 대각 후보가 둘 다 있으면 "공급이 유리한 쪽(세그먼트 상단이 높은 쪽)" 우선
            int leftTop = GetSegmentTopY(leftX, srcY);
            int rightTop = GetSegmentTopY(rightX, srcY);

            if (leftTop > rightTop) chosenX = leftX;
            else if (rightTop > leftTop) chosenX = rightX;
            else
            {
                // 2) 그래도 같으면 기존 diagonalPriority로 타이브레이크(랜덤은 권장하지 않음)
                if (diagonalPriority == 0)
                {
                    // parity 기반(일관성)으로 선택: 랜덤보다 자연스러움
                    chosenX = ((x + emptyY) % 2 == 0) ? leftX : rightX;
                }
                else if (diagonalPriority < 0) chosenX = leftX;
                else chosenX = rightX;
            }
        }
        else
        {
            chosenX = leftOk ? leftX : rightX;
        }

        MoveGemFlow(chosenX, srcY, x, emptyY);
        return true;
    }

    private void ForceSyncGridTransforms()
    {
        if (gems == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Gem g = gems[x, y];
                if (g == null) continue;

                if (g.x != x || g.y != y)
                {
                    g.x = x;
                    g.y = y;
                }

                Transform t = g.transform;
                t.DOKill();
                t.localPosition = GridToLocal(x, y);
                t.localScale = gemBaseScale;
            }
        }
    }

    private void MoveGemFlow(int fromX, int fromY, int toX, int toY)
    {
        if (fromX < 0 || fromX >= width || fromY < 0 || fromY >= height) return;
        if (toX < 0 || toX >= width || toY < 0 || toY >= height) return;

        if (IsIce(toX, toY)) return;

        Gem g = gems[fromX, fromY];
        if (g == null) return;

        if (gems[toX, toY] != null) return;

        gems[fromX, fromY] = null;
        gems[toX, toY] = g;

        int cells = Mathf.Abs(fromY - toY) + Mathf.Abs(fromX - toX);
        float dur = Mathf.Clamp(cells * refillTimePerCell, refillMinTime, refillMaxTime);

        // AnimateFlowMove가 flowMinTime으로 올릴 수 있으니, 대기 시간도 동일 기준으로 보정
        float durClamped = Mathf.Max(dur, flowMinTime);

        Vector3 target = GridToLocal(toX, toY);

        g.ResetVisual();
        g.transform.localScale = gemBaseScale;

        float delay = 0.02f * toY;

        AnimateFlowMove(g.transform, target, durClamped).SetDelay(delay);

        float extra = GetLandingExtraTime();
        lastRefillAnimTime = Mathf.Max(lastRefillAnimTime, delay + durClamped + extra);



        g.x = toX;
        g.y = toY;
    }

    private void FillEmptyCellsBySegments()
    {
        for (int x = 0; x < width; x++)
        {
            int segmentStart = 0;

            for (int y = 0; y < height; y++)
            {
                if (!IsIce(x, y)) continue;

                // ICE 셀은 고정. 비어있으면 안전 생성
                if (gems[x, y] == null)
                    CreateGemAt(x, y);

                // segmentStart ~ y-1 구간에서 "남은 빈칸"만 채우기
                FillEmptyInSegment(x, segmentStart, y - 1, y - 1);

                segmentStart = y + 1;
            }

            FillEmptyInSegment(x, segmentStart, height - 1, height - 1);
        }
    }

    private void FillEmptyInSegment(int x, int startY, int endY, int spawnFromY)
    {
        if (startY > endY) return;

        for (int y = startY; y <= endY; y++)
        {
            if (IsIce(x, y)) continue;
            if (gems[x, y] != null) continue;

            CreateGemAt(x, y, spawnFromY);
        }
    }

    // srcY-1 칸이 막혀있으면(ICE or 다른 젬 or 바닥) 수직 낙하가 막힌 상태로 판단
    private bool IsBlockedBelow(int srcX, int srcY)
    {
        int belowY = srcY - 1;
        if (belowY < 0) return true;
        if (IsIce(srcX, belowY)) return true;
        return gems[srcX, belowY] != null;
    }
    private int GetSegmentTopY(int x, int y)
    {
        // y가 속한 세그먼트의 "최상단 y" (위로 올라가다가 ICE를 만나면 그 직전)
        int top = y;
        for (int yy = y + 1; yy < height; yy++)
        {
            if (IsIce(x, yy)) break;
            top = yy;
        }
        return top;
    }


    private void CollapseAndRefillSegment(int x, int startY, int endY)
    {
        if (startY > endY) return;

        int destY = startY;

        // 1) 내려앉히기(세그먼트 안에서만)
        for (int y = startY; y <= endY; y++)
        {
            if (gems[x, y] == null) continue;

            if (y != destY)
            {
                gems[x, destY] = gems[x, y];

                int cells = y - destY; // 몇 칸 떨어지는지
                float dur = GetFlowDurationByCells(cells);

                //  내려앉히기도 Flow 적용
                gems[x, destY].SetGridPositionFlow(
                    x, destY,
                    dur,
                    flowXSettleRatio,
                    flowEaseY, flowEaseX,
                    flowLandingSquash,
                    flowSquashScaleY,
                    flowSquashTime
                );

                gems[x, y] = null;

            }
            destY++;
        }

        for (int y = destY; y <= endY; y++)
        {
            //  이 세그먼트(endY) 위에서 흘러나오게 (ICE 아래 구간도 자연스럽게)
            CreateGemAt(x, y, endY);
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
                //  null 또는 ICE면 런 끊기
                if (gems[x, y] == null || IsIce(x, y)) { x++; continue; }

                int type = gems[x, y].type;
                int startX = x;
                x++;

                while (x < width && gems[x, y] != null && !IsIce(x, y) && gems[x, y].type == type)
                    x++;

                if (x - startX >= 3) return true;
            }
        }

        // 세로
        for (int x = 0; x < width; x++)
        {
            int y = 0;
            while (y < height)
            {
                //  null 또는 ICE면 런 끊기
                if (gems[x, y] == null || IsIce(x, y)) { y++; continue; }

                int type = gems[x, y].type;
                int startY = y;
                y++;

                while (y < height && gems[x, y] != null && !IsIce(x, y) && gems[x, y].type == type)
                    y++;

                if (y - startY >= 3) return true;
            }
        }

        return false;
    }


    private bool WouldSwapMakeMatch(int x1, int y1, int x2, int y2)
    {
        if (gems == null) return false;

        // ✅ ICE 칸은 스왑 후보/힌트 후보에서 제외
        if (IsIce(x1, y1) || IsIce(x2, y2)) return false;

        Gem g1 = gems[x1, y1];
        Gem g2 = gems[x2, y2];
        if (g1 == null || g2 == null) return false;

        // 타입만 스왑 후, "스왑된 두 칸 주변"에서만 매치가 생겼는지 로컬 검사
        int t1 = g1.type;
        int t2 = g2.type;

        g1.type = t2;
        g2.type = t1;

        bool match = HasMatchAt(x1, y1) || HasMatchAt(x2, y2);

        g1.type = t1;
        g2.type = t2;

        return match;
    }
    private bool HasMatchAt(int x, int y)
    {
        if (gems == null) return false;
        if (x < 0 || x >= width || y < 0 || y >= height) return false;

        if (IsIce(x, y)) return false;
        Gem c = gems[x, y];
        if (c == null) return false;

        int type = c.type;

        // Horizontal
        int count = 1;
        for (int xx = x - 1; xx >= 0; xx--)
        {
            if (IsIce(xx, y)) break;
            Gem g = gems[xx, y];
            if (g == null || g.type != type) break;
            count++;
        }
        for (int xx = x + 1; xx < width; xx++)
        {
            if (IsIce(xx, y)) break;
            Gem g = gems[xx, y];
            if (g == null || g.type != type) break;
            count++;
        }
        if (count >= 3) return true;

        // Vertical
        count = 1;
        for (int yy = y - 1; yy >= 0; yy--)
        {
            if (IsIce(x, yy)) break;
            Gem g = gems[x, yy];
            if (g == null || g.type != type) break;
            count++;
        }
        for (int yy = y + 1; yy < height; yy++)
        {
            if (IsIce(x, yy)) break;
            Gem g = gems[x, yy];
            if (g == null || g.type != type) break;
            count++;
        }

        return count >= 3;
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

                // ✅ ICE 칸은 무브 후보에서 제외
                if (IsIce(x, y)) continue;

                // --- 오른쪽 이웃 후보 ---
                if (x < width - 1)
                {
                    Gem r = gems[x + 1, y];
                    if (r != null && !IsIce(x + 1, y))
                    {
                        // ✅ 특수젬 스왑 즉시 발동 룰: 양쪽 중 하나라도 특수면 무브 가능으로 본다
                        if (g.IsSpecial || r.IsSpecial) return true;

                        // ✅ 일반젬 스왑이 매치를 만드는지 로컬 검사
                        if (WouldSwapMakeMatch(x, y, x + 1, y)) return true;
                    }
                }

                // --- 위쪽 이웃 후보 ---
                if (y < height - 1)
                {
                    Gem u = gems[x, y + 1];
                    if (u != null && !IsIce(x, y + 1))
                    {
                        // ✅ 특수젬 스왑 즉시 발동 룰
                        if (g.IsSpecial || u.IsSpecial) return true;

                        // ✅ 일반젬 스왑 매치 여부
                        if (WouldSwapMakeMatch(x, y, x, y + 1)) return true;
                    }
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
        if (hintGemA != null || hintGemB != null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Gem cur = gems[x, y];
                if (cur == null) continue;
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
        // 셔플은 "젬 배치"만 바꾼다. ICE(블로커)는 절대 재생성/재배치하지 않는다.
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
                Gem g = gems[x, y];
                if (g == null) continue;

                //  ICE 안의 젬은 고정(셔플로 타입도 바꾸지 않음)
                if (IsIce(x, y)) continue;

                //  특수젬은 위치/상태 유지(일반젬만 섞기)
                if (g.IsSpecial) continue;

                targets.Add(g);
                types.Add(g.type);

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

        // 오버레이가 켜지는 프레임 보장
        yield return null;

        Debug.Log($"[ShuffleRoutine] overlayActive={(shuffleOverlay != null && shuffleOverlay.activeSelf)} textActive={(shuffleText != null && shuffleText.gameObject.activeSelf)}");

        yield return new WaitForSeconds(shuffleMessageTime);

        //  여기서 실제 셔플 수행 (재귀 코루틴 호출 금지)
        EnsurePlayableBoardImmediate();

        yield return new WaitForSeconds(0.25f);

        Debug.Log("[ShuffleRoutine] Turn overlay OFF");
        if (shuffleOverlay != null) shuffleOverlay.SetActive(false);
        if (shuffleText != null) shuffleText.gameObject.SetActive(false);

        ClearHint();
        idleTimer = 0f;

        isShuffling = false;

    }
    // 리필 이후 생긴 자연 매치(캐스케이드)를 끝까지 처리한다.
    // - "턴 1회"에서 moves 감소는 이미 상위에서 처리하므로 여기서 moves를 건드리지 않는다.
    private IEnumerator ResolveCascadesAfterRefill()
    {
        int combo = 0;

        while (true)
        {
            int cleared = CheckMatchesAndClear_WithPromotionsSafe();
            if (cleared <= 0) break;

            combo++;
            AddScoreForClear(cleared, comboMultiplier: combo);

            yield return new WaitForSeconds(popDuration);

            yield return StartCoroutine(RefillBoardRoutine());
        }

        if (combo > 0)
            ShowComboBanner(combo);
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
    private void PlaySfx(AudioClip clip, float volumeScale)
    {
        if (audioSource == null) return;
        if (clip == null) return;

        // PlayOneShot은 AudioSource.volume * volumeScale로 최종 볼륨이 결정됨
        audioSource.PlayOneShot(clip, Mathf.Clamp(volumeScale, 0f, 2f));
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

    public void LoadStage(int stageNumber, int w, int h, int goal, int moves)
    {
        // 1) 기본 세팅
        ClearBoard();

        width = w;
        height = h;

        targetScore = goal;
        maxMoves = moves;
        CacheGemBaseScale();
        ResetState();

        // 2) 보드 생성
        gems = new Gem[width, height];
        GenerateBoard();

        // 3) ICE 배열 먼저 초기화 (중요: ApplyIce 전에!)
        InitIceArrays();

        // 4) 스테이지 데이터 기반 ICE 배치 (단일 루트로 통일)
        //    StageManager.CurrentStage가 있다면 그걸 사용
        if (StageManager.Instance != null && StageManager.Instance.CurrentStage != null)
        {
            ApplyIceFromStageData(StageManager.Instance.CurrentStage);
        }
        else
        {
            // StageData 없으면(디버그/단독 실행) stageNumber 기반 예전 방식이 필요하면 여기서만 선택적으로
            // ApplyIceForStage(stageNumber);
            // 지금은 “단일 루트” 원칙이면 그냥 비워도 됨.
        }

        // 5) 시작 1회 보드 검증(즉시 매치 제거 + 무브 확보)
        StartCoroutine(ShuffleRoutine(force: false));

        // 6) UI
        UpdateScoreUI();
        UpdateGoalUI();
        UpdateMovesUI();

        // 7) 보드판 보이게 + 사이즈 갱신
        if (boardPlate != null) boardPlate.gameObject.SetActive(true);
        if (boardGrid != null) boardGrid.gameObject.SetActive(true);

        UpdateBoardPlate();
        AdjustCameraAndBoard();
    }
    // StageManager가 호출하는 4-파라미터 버전(호환용)
    public void LoadStage(int boardWidth, int boardHeight, int goal, int totalMoves)
    {
        // 네가 5-파라미터 LoadStage(스테이지번호 포함)를 쓰고 있다면 그쪽으로 위임
        LoadStage(GetStageNumberSafe(), boardWidth, boardHeight, goal, totalMoves);
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
        iceHp = new int[width, height];

        // ICE "턴 단위" 중복 타격 방지용
        iceHitStamp = new int[width, height];
        iceHitStampId = 1;
    }
    private void BeginIceHitWindow()
    {
        if (!iceLimitToOneHitPerMove) return;
        if (iceHitStamp == null || iceHitStamp.GetLength(0) != width || iceHitStamp.GetLength(1) != height)
            iceHitStamp = new int[width, height];

        iceHitStampId++;
        if (iceHitStampId <= 0) iceHitStampId = 1; // overflow 방어
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

        //  젬과 같은 SortingLayer + 젬보다 높은 order
        int layerId = GetSortingLayerIdFromGemCell(x, y);
        ApplySortingToRenderers(ice, layerId, iceSortingOrder);

        iceObjects[x, y] = ice;
        // ===== Crack 단계 초기화 =====
        // 크랙 스프라이트 0,1,2를 쓰면 maxStage=2, HP=2 (2번 맞으면 제거)
        int maxStage = (iceCrackSprites != null) ? Mathf.Max(0, iceCrackSprites.Length - 1) : 0;
        iceHp[x, y] = maxStage;

        var sr = ice.GetComponent<SpriteRenderer>();
        if (sr != null && iceCrackSprites != null && iceCrackSprites.Length > 0)
        {
            sr.sprite = iceCrackSprites[0]; // 0단계(멀쩡)
        }

    }


    // === ICE hit dedupe: 한 번의 클리어 패스에서 ICE는 1회만 타격 ===
    private void TryBreakIceOnce(int x, int y, bool[,] hitMask)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        if (obstacles == null) return;
        if (obstacles[x, y] != ObstacleType.Ice) return;
        if (iceLimitToOneHitPerMove && iceHitStamp != null)
        {
            if (iceHitStamp[x, y] == iceHitStampId) return;
            iceHitStamp[x, y] = iceHitStampId;
        }
        if (hitMask != null)
        {
            if (hitMask[x, y]) return;
            hitMask[x, y] = true;
        }

        TryBreakIce(x, y);
    }

    // 기존: BreakAdjacentIceAt(int x, int y) → dedupe 버전으로 변경
    private void BreakAdjacentIceAt(int x, int y, bool[,] hitMask)
    {
        TryBreakIceOnce(x + 1, y, hitMask);
        TryBreakIceOnce(x - 1, y, hitMask);
        TryBreakIceOnce(x, y + 1, hitMask);
        TryBreakIceOnce(x, y - 1, hitMask);
    }

    // (안전장치) 혹시 다른 코드가 기존 시그니처를 호출 중이면 컴파일 깨지지 않게 유지
    private void BreakAdjacentIceAt(int x, int y)
    {
        BreakAdjacentIceAt(x, y, null);
    }


    private void TryBreakIce(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        if (obstacles == null) return;
        if (obstacles[x, y] != ObstacleType.Ice) return;

        GameObject ice = iceObjects[x, y];
        if (ice == null)
        {
            // 오브젝트만 유실된 경우: 데이터 정리
            obstacles[x, y] = ObstacleType.None;
            iceHp[x, y] = 0;
            return;
        }

        int maxStage = (iceCrackSprites != null) ? Mathf.Max(0, iceCrackSprites.Length - 1) : 0;

        // 크랙 스프라이트가 없으면 기존처럼 즉시 파괴
        if (maxStage <= 0)
        {
            obstacles[x, y] = ObstacleType.None;
            iceHp[x, y] = 0;
            iceObjects[x, y] = null;

            AddFlatScore(iceBreakScore);
            if (showIceBreakScorePopup)
                SpawnScorePopupAtWorld(iceBreakScore, GridToWorld(x, y));

            StartCoroutine(PlayIceBreakAndDestroy(ice));
            return;
        }

        // HP 초기값 보정(혹시 0으로 남아있을 경우)
        if (iceHp[x, y] <= 0)
            iceHp[x, y] = maxStage;

        // ===== 1타: HP 감소 =====
        iceHp[x, y] = Mathf.Clamp(iceHp[x, y] - 1, 0, maxStage);

        // ===== 단계 스프라이트 적용 =====
        // HP:2 -> index0, HP:1 -> index1, HP:0 -> index2
        int spriteIndex = Mathf.Clamp(maxStage - iceHp[x, y], 0, maxStage);

        var sr = ice.GetComponent<SpriteRenderer>();
        if (sr != null && iceCrackSprites != null && spriteIndex < iceCrackSprites.Length)
            sr.sprite = iceCrackSprites[spriteIndex];
        PlayIceFlash(sr);

        if (spriteIndex == 1 && iceCrack1Clip != null)
            PlaySfx(iceCrack1Clip, 1.35f);   // 중간 크랙: 더 잘 들리게
        else if (spriteIndex == 2 && iceCrack2Clip != null)
            PlaySfx(iceCrack2Clip, 1.45f);   // 완전 크랙: 더 강하게

        // “금 가는” 피드백(파괴 전)
        Transform t = ice.transform;
        t.DOKill();
        t.DOPunchScale(Vector3.one * (icePunchScale * 0.6f), iceCrackStepDelay, 6, 0.6f);

        // ===== 아직 안 깨짐 =====
        if (iceHp[x, y] > 0)
            return;

        // ===== 최종 파괴(HP==0) =====
        if (iceHp[x, y] > 0) return;

        //  완전크랙(2) 프레임을 잠깐 유지하고 최종 파괴
        obstacles[x, y] = ObstacleType.None;
        iceObjects[x, y] = null;

        AddFlatScore(iceBreakScore);
        if (showIceBreakScorePopup)
            SpawnScorePopupAtWorld(iceBreakScore, GridToWorld(x, y));

        StartCoroutine(PlayIceFinalShatterSequence(ice, x, y));
    }
    private IEnumerator PlayIceFinalShatterSequence(GameObject ice, int x, int y)
    {
        // 완전크랙 프레임이 보일 최소 시간 확보
        if (iceFinalCrackHold > 0f)
            yield return new WaitForSeconds(iceFinalCrackHold);

        // 점수/팝업이 TryBreakIce에서 이미 처리중이면 여기서 중복 처리하지 마
        // (너 코드가 어디서 점수를 주는지에 따라 한쪽만 유지)

        StartCoroutine(PlayIceBreakAndDestroy(ice));
    }


    private int GetSortingLayerIdFromGemCell(int x, int y)
    {
        if (gems != null && x >= 0 && x < width && y >= 0 && y < height)
        {
            Gem g = gems[x, y];
            if (g != null && g.sr != null) return g.sr.sortingLayerID;
        }
        return 0; // Default
    }

    private void ApplySortingToRenderers(GameObject root, int sortingLayerId, int sortingOrder)
    {
        if (root == null) return;

        // SpriteRenderer
        var srs = root.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            sr.sortingLayerID = sortingLayerId;
            sr.sortingOrder = sortingOrder;
        }

        // ParticleSystemRenderer
        var prs = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
        foreach (var pr in prs)
        {
            pr.sortingLayerID = sortingLayerId;
            pr.sortingOrder = sortingOrder;
        }
    }


    private IEnumerator PlayIceBreakAndDestroy(GameObject ice)
    {
        if (ice == null) yield break;

        //  ice의 sortingLayer를 기준으로 FX를 더 위에 올림
        int layerId = 0;
        var iceSr = ice.GetComponent<SpriteRenderer>();
        if (iceSr != null) layerId = iceSr.sortingLayerID;

        if (iceBreakFxPrefab != null)
        {
            GameObject fx = Instantiate(iceBreakFxPrefab, ice.transform.position, Quaternion.identity);
            ApplySortingToRenderers(fx, layerId, iceBreakFxSortingOrder);
        }

        //  최종 파괴 사운드(우선순위: iceShatterClip, 없으면 iceBreakClip)
        if (iceShatterClip != null) PlaySfx(iceShatterClip);
        else if (iceBreakClip != null) PlaySfx(iceBreakClip);

        //  카메라 흔들림(최종 파괴 때만)
        ShakeCameraForIce();


        Transform t = ice.transform;
        t.DOKill();

        // 펀치(깨짐 느낌)
        t.DOPunchScale(Vector3.one * icePunchScale, iceBreakDuration, 6, 0.6f);

        if (iceSr != null)
        {
            iceSr.DOKill();
            iceSr.DOFade(0f, iceBreakDuration);
        }

        yield return new WaitForSeconds(iceBreakDuration);

        if (ice != null) Destroy(ice);
    }
    

    private int GetStageNumberSafe()
    {
        // StageManager 기준: currentStageIndex는 0-based (Stage 1 = 0)
        if (StageManager.Instance != null)
        {
            // CurrentStage가 null이어도 currentStageIndex 자체는 유효할 수 있으니 그대로 사용
            return StageManager.Instance.currentStageIndex + 1;
        }

        // StageManager가 없는 씬(테스트 씬 등)에서는 1로 처리
        return 1;
    }


    private void ApplyIceForStage(int stageNumber)
    {
        if (stageNumber < 4) return;
        if (obstacles == null || iceObjects == null) InitIceArrays();

        if (!useRandomIce)
        {
            // 수동 리스트(디버그/테스트용) 유지하고 싶으면 사용
            if (stage4IceCells != null)
                foreach (var c in stage4IceCells)
                    PlaceIceAt(c.x, c.y);

            return;
        }

        int targetCount = iceCountStage4 + (stageNumber - 4) * iceCountPerStage;
        targetCount = Mathf.Clamp(targetCount, 0, iceMaxCount);

        int seed = deterministicByStage ? (stageNumber * 1000 + randomSeedOffset) : Random.Range(int.MinValue, int.MaxValue);
        PlaceRandomIce(targetCount, seed);
    }

    private void PlaceRandomIce(int count, int seed)
    {
        if (count <= 0) return;

        List<Vector2Int> candidates = new List<Vector2Int>(width * height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (IsIce(x, y)) continue;
                if (gems != null && gems[x, y] == null) continue;
                candidates.Add(new Vector2Int(x, y));
            }
        }

        if (candidates.Count == 0) return;

        // 결정적 셔플
        System.Random rng = new System.Random(seed);
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int placed = 0;

        // 1차: 룰(클러스터 제한) 적용
        for (int i = 0; i < candidates.Count && placed < count; i++)
        {
            var c = candidates[i];
            if (!CanPlaceIceAtWithRules(c.x, c.y)) continue;

            PlaceIceAt(c.x, c.y);
            placed++;
        }

        // 2차(보험): 룰 때문에 덜 깔리면, 제한을 일부 완화하고 채움
        // (캔디크러시 느낌은 유지하되 “스테이지 난이도/지시 개수”는 맞추기)
        if (placed < count)
        {
            for (int i = 0; i < candidates.Count && placed < count; i++)
            {
                var c = candidates[i];
                if (IsIce(c.x, c.y)) continue;

                // 완화 조건: 2x2만 방지하고 나머지는 허용
                if (avoidIce2x2 && WouldForm2x2Ice(c.x, c.y)) continue;

                PlaceIceAt(c.x, c.y);
                placed++;
            }
        }
    }

    private int CountAdjacentIce4(int x, int y)
    {
        int c = 0;
        if (IsIce(x + 1, y)) c++;
        if (IsIce(x - 1, y)) c++;
        if (IsIce(x, y + 1)) c++;
        if (IsIce(x, y - 1)) c++;
        return c;
    }

    private bool WouldForm2x2Ice(int x, int y)
    {
        // (x,y)를 포함하는 2x2 사각형 4가지 검사
        for (int dx = -1; dx <= 0; dx++)
        {
            for (int dy = -1; dy <= 0; dy++)
            {
                int ax = x + dx;
                int ay = y + dy;
                if (ax < 0 || ay < 0 || ax + 1 >= width || ay + 1 >= height) continue;

                bool allIce = true;
                for (int sx = 0; sx <= 1; sx++)
                {
                    for (int sy = 0; sy <= 1; sy++)
                    {
                        int nx = ax + sx;
                        int ny = ay + sy;

                        // 후보 칸은 “놓인 것으로” 가정
                        if (nx == x && ny == y) continue;

                        if (!IsIce(nx, ny))
                        {
                            allIce = false;
                            break;
                        }
                    }
                    if (!allIce) break;
                }

                if (allIce) return true;
            }
        }
        return false;
    }

    private bool WouldExceedClusterSize(int x, int y, int maxSize)
    {
        // 후보 (x,y)를 Ice로 가정하고, 연결 컴포넌트 크기 계산
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        HashSet<int> visited = new HashSet<int>();

        q.Enqueue(new Vector2Int(x, y));
        visited.Add(x + y * width);

        int size = 0;

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            size++;
            if (size > maxSize) return true;

            void TryEnqueue(int nx, int ny)
            {
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) return;

                bool isIce = (nx == x && ny == y) || IsIce(nx, ny);
                if (!isIce) return;

                int key = nx + ny * width;
                if (visited.Add(key))
                    q.Enqueue(new Vector2Int(nx, ny));
            }

            TryEnqueue(p.x + 1, p.y);
            TryEnqueue(p.x - 1, p.y);
            TryEnqueue(p.x, p.y + 1);
            TryEnqueue(p.x, p.y - 1);
        }

        return false;
    }

    private bool CanPlaceIceAtWithRules(int x, int y)
    {
        if (!limitIceClustering) return true;

        if (iceMaxAdjacent >= 0)
        {
            int adj = CountAdjacentIce4(x, y);
            if (adj > iceMaxAdjacent) return false;
        }

        if (avoidIce2x2 && WouldForm2x2Ice(x, y)) return false;

        if (iceMaxClusterSize > 0 && WouldExceedClusterSize(x, y, iceMaxClusterSize)) return false;

        return true;
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
        BeginIceHitWindow();
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
            AddScoreForClear(totalCleared, comboMultiplier: 1);

            movesLeft--;
            UpdateMovesUI();

            if (score >= targetScore) { EndGame(true); yield break; }
            if (movesLeft <= 0) { EndGame(false); yield break; }

            yield return new WaitForSeconds(popDuration);
            yield return StartCoroutine(PostClearRefillAndEnsure());
            yield break;
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

        //  이번 ClearByMaskWithChain 패스에서 ICE는 1회만 타격되도록 기록
        bool[,] iceHitThisPass = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!finalMask[x, y]) continue;
                if (gems[x, y] == null) continue;

                //  ICE 셀 자체가 마스크에 포함되면: 젬 삭제 대신 ICE에 "1회만" 타격
                if (IsIce(x, y))
                {
                    TryBreakIceOnce(x, y, iceHitThisPass);
                    continue;
                }

                //  일반 젬 삭제 시: 인접 ICE 타격(여기도 "1회만" 적용되도록 dedupe)
                BreakAdjacentIceAt(x, y, iceHitThisPass);

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
