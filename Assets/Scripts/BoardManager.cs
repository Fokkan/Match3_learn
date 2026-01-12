using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;



public class BoardManager : MonoBehaviour
{
    #region Serialized Fields

    [Header("Camera Settings")]
    public Camera mainCamera;
    public float boardTopMargin = 2f;
    public float boardSideMargin = 0.5f;

    [Header("UI-World Layout Bridge")]
    [SerializeField] private RectTransform boardWorldAnchorUI; // Canvas/MiddleArea 아래 BoardWorldAnchorUI
    [SerializeField] private Transform boardRoot;              // 월드의 Board 오브젝트(루트)
    [SerializeField] private Vector3 boardWorldOffset;         // 필요 시 미세 보정 (0,0,0부터 시작)


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

    public enum LevelGoalType
    {
        ScoreOnly,
        ClearAllIce,
        CollectColor,
        CollectMultiColor,                 
        ClearAllIceAndCollectMultiColor,
        ScoreAndClearAllIce,
        ScoreAndCollectColor
    }
    

    [Header("Goal Stars Options")]
    [SerializeField] private bool hideStarsUntilReached = true;

    [SerializeField] private float starPopDuration = 0.22f;
    [SerializeField] private float starPopScale = 1.15f;

    [SerializeField] private AudioClip starReachedClip;

    [Header("Stage Clear Bonus (Move -> Random Explosions)")]
    [SerializeField] private bool enableStageClearMoveBonus = true;
    [SerializeField] private float stageClearBonusStepDelay = 0.06f;
    [SerializeField, Range(0, 100)] private int stageClearBonusMakeSpecialChance = 70;
    [SerializeField] private bool stageClearBonusAllowWrapped = true;
    [SerializeField] private bool stageClearBonusAlsoDetonateRemainingSpecials = true;

    // 레벨 클리어 후 보너스 연출에서는 ICE까지 무시하고 터뜨릴지(원하면 true)
    [SerializeField] private bool stageClearBonusIgnoreIceForDetonation = false;

    // 마지막 클리어에서 "보너스 연출로 추가된 점수" 표시용
    private int lastClearBonusScore = 0;

    public enum InGameBoosterType
    {
        None,
        Blast3x3,
        CrossRowCol,
        FreeSwitch,
        RandomSpecial5
    }

    [Header("In-Game Boosters (Bottom HUD)")]
    [SerializeField] private bool enableBoosters = true;

    // 부스터별 Move 소모 여부(캔디크러시 감성 기본값: false)
    [SerializeField] private bool booster1ConsumesMove = false; // 3x3
    [SerializeField] private bool booster2ConsumesMove = false; // 십자
    [SerializeField] private bool booster3ConsumesMove = false; // 맘대로 바꾸기
    [SerializeField] private bool booster4ConsumesMove = false; // 랜덤 특수 변환

    // UI Hook
    [SerializeField] private Button booster1Button;
    [SerializeField] private TMP_Text booster1CountText;
    [SerializeField] private GameObject booster1SelectedMark;
    [SerializeField] private int booster1StartCount = 3;

    [SerializeField] private Button booster2Button;
    [SerializeField] private TMP_Text booster2CountText;
    [SerializeField] private GameObject booster2SelectedMark;
    [SerializeField] private int booster2StartCount = 3;

    [SerializeField] private Button booster3Button;
    [SerializeField] private TMP_Text booster3CountText;
    [SerializeField] private GameObject booster3SelectedMark;
    [SerializeField] private int booster3StartCount = 2;

    [SerializeField] private Button booster4Button;
    [SerializeField] private TMP_Text booster4CountText;
    [SerializeField] private int booster4StartCount = 1;
    [SerializeField] private GameObject booster4SelectedMark;

    [Header("Booster Selection FX")]
    [SerializeField] private float boosterSelectedPulseScale = 1.08f;
    [SerializeField] private float boosterSelectedPulseDuration = 0.35f;
    [SerializeField] private Ease boosterSelectedPulseEase = Ease.InOutSine;
    [Header("Booster 4 Convert FX")]
    [SerializeField] private float booster4PrePunch = 0.18f;
    [SerializeField] private float booster4PrePunchDuration = 0.18f;
    [SerializeField] private float booster4PostPopScale = 1.12f;
    [SerializeField] private float booster4PostPopDuration = 0.10f;
    [SerializeField] private float booster4StaggerDelay = 0.06f;   // 변환 사이 템포(순차)
    [SerializeField] private float booster4VanishScale = 0.85f;    // 사라지기(축소) 비율
    [SerializeField] private float booster4VanishDuration = 0.10f; // 축소 시간


    // 부스터4 변환 옵션
    [SerializeField] private bool booster4AllowWrapped = true;
    [SerializeField] private bool booster4AllowColorBomb = false;

    [SerializeField] private AudioClip boosterSelectClip;
    [SerializeField] private AudioClip boosterUseClip;

    [Header("Booster SelectedMark FX")]
    [SerializeField] private float boosterMarkPulseScale = 1.08f;
    [SerializeField] private float boosterMarkPulseDuration = 0.35f;
    [SerializeField] private float boosterMarkAlphaMin = 0.55f;


    [Header("Booster Target Mark")]
    [SerializeField] private GameObject boosterTargetMarkPrefab; // 링/하이라이트 프리팹
    [SerializeField] private int boosterTargetSortingOrderOffset = 20;
    [SerializeField] private float boosterTargetPulseScale = 1.08f;
    [SerializeField] private float boosterTargetPulseDuration = 0.25f;
    [SerializeField] private float boosterTargetAlphaMin = 0.55f;

    [SerializeField] private float boosterTargetConfirmScale = 1.22f;
    [SerializeField] private float boosterTargetConfirmDuration = 0.10f;

    [SerializeField] private float boosterSwapPunchScale = 0.10f;   // 젬이 ‘툭’ 하는 정도
    [SerializeField] private float boosterSwapPunchDuration = 0.12f;


    [Header("Level Goal")]
    public LevelGoalType levelGoalType = LevelGoalType.ScoreOnly;

    // 캔디크러시처럼 “Objective + 최소 점수(1-star)” 느낌을 원하면 true
    public bool requirePassScore = true;

    // CollectColor용 (gemSprites index 기반)
    public int collectGemType = 0;
    public int collectTarget = 20;
    private int[] collectGemTypesMulti = null;
    private int[] collectTargetsMulti = null;
    private int[] collectedMulti = null;

    // Goal UI에서 진행도 갱신을 받을 수 있도록 이벤트 제공
    public event Action OnGoalProgressChanged;

    private void NotifyGoalProgressChanged()
    {
        OnGoalProgressChanged?.Invoke();
    }

    // Collect(단일/멀티) 목표 데이터를 UI가 읽을 수 있게 제공
    // - 단일 CollectColor도 멀티 형태(길이 1 배열)로 반환
    public bool TryGetCollectGoalData(out int[] gemTypes, out int[] targets, out int[] collected)
    {
        gemTypes = null;
        targets = null;
        collected = null;

        // 단일 Collect
        if (levelGoalType == LevelGoalType.CollectColor || levelGoalType == LevelGoalType.ScoreAndCollectColor)
        {
            gemTypes = new[] { collectGemType };
            targets = new[] { collectTarget };
            collected = new[] { collectedCount };
            return true;
        }

        // 멀티 Collect
        if (levelGoalType == LevelGoalType.CollectMultiColor || levelGoalType == LevelGoalType.ClearAllIceAndCollectMultiColor)
        {
            if (collectGemTypesMulti == null || collectTargetsMulti == null || collectedMulti == null) return false;

            gemTypes = (int[])collectGemTypesMulti.Clone();
            targets = (int[])collectTargetsMulti.Clone();
            collected = (int[])collectedMulti.Clone();
            return true;
        }

        return false;
    }

    // ICE 남은 개수
    public int GetIceRemaining()
    {
        return Mathf.Max(0, totalIce - clearedIce);
    }

    public int GetTotalIce()
    {
        return totalIce;
    }


    // 런타임 진행도
    private int collectedCount = 0;
    private int totalIce = 0;
    private int clearedIce = 0;

    private void ResetGoalProgress()
    {
        collectedCount = 0;
        clearedIce = 0;

        if (collectedMulti != null)
        {
            Array.Clear(collectedMulti, 0, collectedMulti.Length);
        }
        NotifyGoalProgressChanged();
    }


    private void RecountTotalIce()
    {
        totalIce = 0;
        if (obstacles == null) return;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (obstacles[x, y] == ObstacleType.Ice) totalIce++;
    }

    private bool IsStageCleared()
    {
        switch (levelGoalType)
        {
            // 점수만 보는 스테이지
            case LevelGoalType.ScoreOnly:
                return score >= passScore;

            // 얼음만 깨면 클리어 (옵션으로 최소 점수도 요구 가능)
            case LevelGoalType.ClearAllIce:
                if (requirePassScore && score < passScore) return false;
                return clearedIce >= totalIce;

            // Collect 단일
            case LevelGoalType.CollectColor:
                return collectedCount >= collectTarget;

            // Collect 멀티(2~4색)
            case LevelGoalType.CollectMultiColor:
                {
                    if (collectTargetsMulti == null || collectedMulti == null) return false;
                    for (int i = 0; i < collectTargetsMulti.Length; i++)
                    {
                        if (collectedMulti[i] < collectTargetsMulti[i]) return false;
                    }
                    return true;
                }

            // ICE + Collect 멀티
            // 네 요구사항: “Collect만 달성하면 클리어”라면 ICE도 같이 달성해야 한다는 전제가 들어가므로,
            // 이 타입은 ICE + Collect 둘 다 만족해야 true가 맞다.
            case LevelGoalType.ClearAllIceAndCollectMultiColor:
                {
                    if (clearedIce < totalIce) return false;

                    if (collectTargetsMulti == null || collectedMulti == null) return false;
                    for (int i = 0; i < collectTargetsMulti.Length; i++)
                    {
                        if (collectedMulti[i] < collectTargetsMulti[i]) return false;
                    }
                    return true;
                }

            // 점수 + 얼음
            case LevelGoalType.ScoreAndClearAllIce:
                return (score >= passScore) && (clearedIce >= totalIce);

            // 점수 + Collect 단일
            case LevelGoalType.ScoreAndCollectColor:
                return (score >= passScore) && (collectedCount >= collectTarget);

            default:
                // 안전장치: 기존처럼 최소 점수 요구 옵션이 켜져 있으면 막음
                if (requirePassScore && score < passScore) return false;
                return score >= passScore;
        }
    }


    private float GetGoalProgress01()
    {
        float ScoreP() => (targetScore <= 0) ? 1f : Mathf.Clamp01((float)score / targetScore);
        float IceP() => (totalIce <= 0) ? 1f : Mathf.Clamp01((float)clearedIce / totalIce);
        float CollectP() => (collectTarget <= 0) ? 1f : Mathf.Clamp01((float)collectedCount / collectTarget);
        float MultiCollectP()
        {
            if (collectTargetsMulti == null || collectedMulti == null) return 0f;
            if (collectTargetsMulti.Length == 0) return 1f;

            float p = 1f;
            for (int i = 0; i < collectTargetsMulti.Length; i++)
            {
                int tgt = collectTargetsMulti[i];
                float r = (tgt <= 0) ? 1f : Mathf.Clamp01((float)collectedMulti[i] / tgt);
                p = Mathf.Min(p, r);
            }
            return p;
        }


        switch (levelGoalType)
        {
            case LevelGoalType.ScoreOnly: return ScoreP();
            case LevelGoalType.ClearAllIce: return IceP();
            case LevelGoalType.CollectColor: return CollectP();
            case LevelGoalType.ScoreAndClearAllIce: return Mathf.Min(ScoreP(), IceP());
            case LevelGoalType.ScoreAndCollectColor: return Mathf.Min(ScoreP(), CollectP());
            case LevelGoalType.CollectMultiColor: return MultiCollectP();
            case LevelGoalType.ClearAllIceAndCollectMultiColor: return Mathf.Min(IceP(), MultiCollectP());

            default: return ScoreP();
        }
    }

    private void UpdateGoalTextUI()
    {
        if (goalText == null) return;

        switch (levelGoalType)
        {
            case LevelGoalType.ScoreOnly:
                goalText.text = $"Goal: {targetScore}";
                break;

            case LevelGoalType.ClearAllIce:
                goalText.text = $"Goal: Break Ice {clearedIce}/{totalIce}";
                break;

            case LevelGoalType.CollectColor:
                goalText.text = $"Goal: Collect {collectedCount}/{collectTarget}";
                break;

            case LevelGoalType.ScoreAndClearAllIce:
                goalText.text = $"Goal: Ice {clearedIce}/{totalIce}  Score {score}/{targetScore}";
                break;

            case LevelGoalType.ScoreAndCollectColor:
                goalText.text = $"Goal: Collect {collectedCount}/{collectTarget}  Score {score}/{targetScore}";
                break;
            case LevelGoalType.CollectMultiColor:
                {
                    if (collectTargetsMulti == null || collectedMulti == null)
                    {
                        goalText.text = "Goal: Collect (Not Set)";
                        break;
                    }

                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("Goal: Collect ");
                    for (int i = 0; i < collectTargetsMulti.Length; i++)
                    {
                        if (i > 0) sb.Append(" | ");
                        sb.Append($"T{collectGemTypesMulti[i]} {collectedMulti[i]}/{collectTargetsMulti[i]}");
                    }
                    goalText.text = sb.ToString();
                    break;
                }

            case LevelGoalType.ClearAllIceAndCollectMultiColor:
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append($"Goal: Ice {clearedIce}/{totalIce}  ");

                    if (collectTargetsMulti == null || collectedMulti == null)
                    {
                        sb.Append("Collect (Not Set)");
                    }
                    else
                    {
                        sb.Append("Collect ");
                        for (int i = 0; i < collectTargetsMulti.Length; i++)
                        {
                            if (i > 0) sb.Append(" | ");
                            sb.Append($"T{collectGemTypesMulti[i]} {collectedMulti[i]}/{collectTargetsMulti[i]}");
                        }
                    }

                    goalText.text = sb.ToString();
                    break;
                }

        }
    }


    [Header("Game Rule UI")]
    public TMP_Text goalText;
    public TMP_Text movesText;
    public GameObject gameOverPanel;
    public TMP_Text resultText;

    [Header("Goal Gauge UI")]
    [SerializeField] private Image goalFillImage;
    [SerializeField] private Image star1Image;
    [SerializeField] private Image star2Image;
    [SerializeField] private Image star3Image;

    [SerializeField, Range(0f, 1f)] private float star1Percent = 0.33f;
    [SerializeField, Range(0f, 1f)] private float star2Percent = 0.66f;
    [SerializeField, Range(0f, 1f)] private float star3Percent = 1.00f;

    private bool star1Shown = false;
    private bool star2Shown = false;
    private bool star3Shown = false;


    [Header("Star Thresholds")]
    public float star2Multiplier = 1.3f;
    public float star3Multiplier = 1.6f;
    public float starOffAlpha = 0.25f;
    

    private int passScore;
    private int star2Score;
    private int star3Score;

    // 네가 코드에서 이미 star1Unlocked 같은 이름을 쓰고 있다면 이 이름 그대로 둔다
    private bool star1Unlocked;
    private bool star2Unlocked;
    private bool star3Unlocked;

    // 네 코드가 PassScore(대문자)를 직접 참조한다면, 아래 프로퍼티로 호환 처리
    private int PassScore => passScore;

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
    // ===== In-Game Boosters Runtime =====
    private InGameBoosterType activeBooster = InGameBoosterType.None;

    private int booster1Count;
    private int booster2Count;
    private int booster3Count;
    private int booster4Count;
    // Booster click targets (Boosters 1~2 use single target, Booster 3 uses two picks)
    private Gem boosterTargetGem = null;
    private Gem freeSwitchSecondPick = null;
    private Gem boosterTargetPick = null; // Booster1/2: 1클릭 타겟(링)용


    // SelectedMark pulse tweens
    private Tween booster1MarkTween;
    private Tween booster2MarkTween;
    private Tween booster3MarkTween;
    private Tween booster4MarkTween;

    private Gem freeSwitchFirstPick = null;
    // Booster target mark instances (A=주 타겟, B=FreeSwitch 두 번째 타겟)
    private GameObject boosterTargetMarkA;
    private GameObject boosterTargetMarkB;
    private Tween boosterTargetTweenA;
    private Tween boosterTargetTweenB;
    // Booster 1/2: 타겟 2클릭 확정용(좌표 기반)
    private bool booster12HasTarget = false;
    private int booster12TargetX = -1;
    private int booster12TargetY = -1;




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
    [System.Serializable]
    private struct IcePlacementRules
    {
        public bool avoidTopRow;
        public bool avoidBottomRow;
        public bool avoidLeftCol;
        public bool avoidRightCol;
        public bool avoidSpawns;     // 현재 프로젝트에서는 TopRow 회피와 동일 취급
        public bool avoidLocked;     // (현재 BoardManager에 locked 개념이 없으면 필터링은 생략 가능)
        public bool avoidObstacles;  // 이미 obstacles[x,y] != None이면 후보에서 제외됨(안전망)

        public bool avoid2x2;        // 기존 기능 유지
    }

    /// <summary>
    /// StageData에 정의된 iceCage / obstacleCount / obstacleLevel로 ICE를 배치한다.
    /// 우선순위: iceCage(고정) > obstacleCount(랜덤)
    /// </summary>

    private void ApplyIceFromStageData(StageData s)
    {
        // ICE 시스템이 없는 빌드/씬에서도 크래시 나지 않게 방어
        if (s == null) return;

        // (1) 수동 배치 마스크가 있으면 우선 적용 (TopLeftOrigin 기준)
        // StageData.cs에서 iceCage는 int[] 입니다. (0=없음, 1=얼음 등)
        if (s.iceCage != null && s.iceCage.Length == width * height)
        {
            ApplyIceCageMask_TopLeftOrigin(s.iceCage);
            return;
        }

        // (2) 랜덤 배치(개수 기반)
        if (s.obstacleCount <= 0) return;

        // 네 프로젝트 기준 ICE 규칙 타입은 IceClusterRules가 정식입니다.
        IceClusterRules rules = GetIceRulesFromLevel(s.obstacleLevel);

        // 결정적 랜덤(같은 스테이지면 같은 배치) 권장: seed에 stageID 활용
        int seed = s.stageID * 1000 + 12345;
        PlaceRandomIceWithRules(s.obstacleCount, seed, rules);
    }


    // BoardManager.cs
    // 위치: private void ApplyGoalFromStageData(StageData s)  <-- 이 함수 전체를 교체
    private void ApplyGoalFromStageData(StageData s)
    {
        // 기본값: 인스펙터 설정 유지(디버그/단독 실행 대비)
        collectGemTypesMulti = null;
        collectTargetsMulti = null;
        collectedMulti = null;

        if (s == null) return;

        // 스테이지에서 Collect 목표를 쓰지 않으면 기존 goalType 유지
        if (!s.useCollectGoal || s.collectTargets == null || s.collectTargets.Count == 0)
            return;

        // 스테이지 단위로 requirePassScore를 오버라이드
        requirePassScore = s.requirePassScore;

        // 핵심: 같은 gemType이 여러 번 들어오면 "제거"가 아니라 "합산"한다.
        Dictionary<int, int> summed = new Dictionary<int, int>(8);
        List<int> order = new List<int>(4); // 최초 등장 순서 유지(표시 순서 고정)

        for (int i = 0; i < s.collectTargets.Count; i++)
        {
            int t = s.collectTargets[i].gemType;
            int goal = s.collectTargets[i].target;

            if (goal <= 0) continue;
            if (t < 0) continue;
            if (gemSprites != null && t >= gemSprites.Length) continue;

            if (!summed.ContainsKey(t))
            {
                summed[t] = goal;
                order.Add(t);
            }
            else
            {
                summed[t] += goal;
            }
        }

        if (order.Count == 0) return;

        int n = Mathf.Min(4, order.Count);

        collectGemTypesMulti = new int[n];
        collectTargetsMulti = new int[n];
        collectedMulti = new int[n];

        for (int i = 0; i < n; i++)
        {
            int t = order[i];
            collectGemTypesMulti[i] = t;
            collectTargetsMulti[i] = summed[t];
        }

        // 블로커(ICE) 스테이지면: ICE AND Collect
        // totalIce는 RecountTotalIce() 이후 값이므로, ApplyGoalFromStageData는 Recount 이후에 호출되어야 함.
        if (totalIce > 0)
            levelGoalType = LevelGoalType.ClearAllIceAndCollectMultiColor;
        else
            levelGoalType = LevelGoalType.CollectMultiColor;
    }




    private void AlignBoardToMiddleArea()
    {
        if (boardWorldAnchorUI == null) return;
        if (boardRoot == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, boardWorldAnchorUI.position);

        float z = Mathf.Abs(cam.transform.position.z - boardRoot.position.z);
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));

        boardRoot.position = worldPos + boardWorldOffset;
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

    private static BoardManager _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // DontDestroyOnLoad(gameObject); // 유지할 이유 없으면 제거
    }


    private void Start()
    {
        Debug.Log($"[DEBUG] timeScale={Time.timeScale}");

        CacheGemBaseScale();
        if (StageManager.Instance == null || StageManager.Instance.CurrentStage == null)
        {
            maxMoves = defaultMaxMoves;
            ResetState();

            gems = new Gem[width, height];
            InitIceArrays();
            GenerateBoard();
            ApplyIceFromStageData(StageManager.Instance != null ? StageManager.Instance.CurrentStage : null);
            RecountTotalIce();
            ResetGoalProgress();
            UpdateGoalTextUI();
            UpdateGoalGaugeUI();


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
    private void UpdateGoalProgressUI()
    {
        if (targetScore <= 0) return;

        float p = Mathf.Clamp01((float)score / targetScore);

        if (goalFillImage != null)
            goalFillImage.fillAmount = p;

        TryShowStar(ref star1Shown, star1Image, p >= star1Percent);
        TryShowStar(ref star2Shown, star2Image, p >= star2Percent);
        TryShowStar(ref star3Shown, star3Image, p >= star3Percent);
    }

    private void TryShowStar(ref bool alreadyShown, Image starImg, bool shouldShow)
    {
        if (starImg == null) return;

        if (!shouldShow) return;

        if (alreadyShown) return;

        alreadyShown = true;
        starImg.gameObject.SetActive(true);

        // 연출은 일단 단순 활성화만. 원하면 여기서 DOScale 팝 연출 추가 가능.
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

        UpdateGoalGaugeUI();
    }

    private void UpdateGoalUI()
    {
        if (goalFillImage == null) return;
        if (targetScore <= 0) return;

        float t = Mathf.Clamp01((float)score / targetScore);
        goalFillImage.fillAmount = t;

        TryShowStar(t, star1Percent, ref star1Shown, star1Image);
        TryShowStar(t, star2Percent, ref star2Shown, star2Image);
        TryShowStar(t, star3Percent, ref star3Shown, star3Image);
    }

    private void TryShowStar(float t, float percent, ref bool shownFlag, Image starImg)
    {
        if (starImg == null) return;

        if (hideStarsUntilReached)
        {
            if (!shownFlag) starImg.gameObject.SetActive(false);
        }
        else
        {
            starImg.gameObject.SetActive(true);
        }

        if (shownFlag) return;
        if (t + 0.0001f < percent) return;

        shownFlag = true;
        starImg.gameObject.SetActive(true);

        starImg.transform.DOKill();
        starImg.transform.localScale = Vector3.one;
        starImg.transform
            .DOScale(Vector3.one * starPopScale, starPopDuration * 0.5f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                starImg.transform.DOScale(Vector3.one, starPopDuration * 0.5f).SetEase(Ease.OutQuad);
            });

        if (starReachedClip != null) PlaySfx(starReachedClip);
    }


    private void SetupStarThresholds()
    {
        passScore = Mathf.Max(1, targetScore);
        star2Score = Mathf.Max(passScore, Mathf.CeilToInt(passScore * star2Multiplier));
        star3Score = Mathf.Max(star2Score, Mathf.CeilToInt(passScore * star3Multiplier));

        star1Unlocked = false;
        star2Unlocked = false;
        star3Unlocked = false;

        ApplyStarVisual(star1Image, false);
        ApplyStarVisual(star2Image, false);
        ApplyStarVisual(star3Image, false);

        if (goalFillImage != null)
            goalFillImage.fillAmount = 0f;
    }

    private void ApplyStarVisual(Image img, bool on)
    {
        if (img == null) return;

        var c = img.color;
        c.a = on ? 1f : starOffAlpha;
        img.color = c;
    }

    


    private void UpdateMovesUI()
    {
        if (movesText != null)
            movesText.text = $"Moves: {movesLeft}";
    }

    private void ResetGoalGaugeUI()
    {
        star1Shown = false;
        star2Shown = false;
        star3Shown = false;

        if (goalFillImage != null)
            goalFillImage.fillAmount = 0f;

        if (!hideStarsUntilReached) return;

        if (star1Image != null) star1Image.gameObject.SetActive(false);
        if (star2Image != null) star2Image.gameObject.SetActive(false);
        if (star3Image != null) star3Image.gameObject.SetActive(false);
    }

    private void UpdateGoalGaugeUI()
    {
        // 점수 기반 진행도(= Goal 게이지)
        if (targetScore <= 0)
        {
            if (goalFillImage != null) goalFillImage.fillAmount = 0f;
            return;
        }

        float p = Mathf.Clamp01((float)score / targetScore);

        if (goalFillImage != null)
            goalFillImage.fillAmount = p;

        HandleStarReached(star1Image, star1Percent, ref star1Shown, p);
        HandleStarReached(star2Image, star2Percent, ref star2Shown, p);
        HandleStarReached(star3Image, star3Percent, ref star3Shown, p);
    }



    private void HandleStarReached(Image star, float threshold, ref bool shown, float progress)
    {
        if (star == null) return;

        if (shown) return;

        if (progress + 0.0001f < threshold) return;

        shown = true;

        if (hideStarsUntilReached)
            star.gameObject.SetActive(true);

        ApplyStarVisual(star, true);

        PopStar(star);

        if (starReachedClip != null)
            PlaySfx(starReachedClip);
    }

    private void PopStar(Image star)
    {
        RectTransform rt = star.rectTransform;
        if (rt == null) return;

        rt.DOKill();

        Vector3 baseScale = rt.localScale;
        rt.localScale = baseScale * 0.6f;

        rt.DOScale(baseScale * starPopScale, starPopDuration * 0.55f)
          .SetEase(Ease.OutBack)
          .OnComplete(() =>
          {
              rt.DOScale(baseScale, starPopDuration * 0.45f).SetEase(Ease.OutQuad);
          });
    }

    #endregion

    #region Input Handling

    public void OnGemClicked(Gem gem)
    {
        if (isGameOver) return;
        if (isShuffling) return;
        if (isAnimating) return;
        if (gem == null) return;
        // 부스터 모드가 켜져 있으면, 일반 선택/스왑 대신 부스터 처리
        if (enableBoosters && activeBooster != InGameBoosterType.None)
        {
            HandleBoosterClick(gem);
            return;
        }

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
    // ===== Boosters: UI OnClick =====
    public void OnClickBooster1_Blast3x3() => ToggleBooster(InGameBoosterType.Blast3x3);
    public void OnClickBooster2_CrossRowCol() => ToggleBooster(InGameBoosterType.CrossRowCol);
    public void OnClickBooster3_FreeSwitch() => ToggleBooster(InGameBoosterType.FreeSwitch);

    public void OnClickBooster4_RandomSpecial5()
    {
        if (!enableBoosters) return;
        if (isGameOver || isShuffling || isAnimating) return;
        if (booster4Count <= 0) return;

        StartCoroutine(UseBooster4_RandomSpecial5Routine());
    }

    // ===== Boosters: Init/State =====
    [SerializeField] private bool autoBindBoosterButtons = true;
    private void InitBoostersForStage()
    {
        booster1Count = Mathf.Max(0, booster1StartCount);
        booster2Count = Mathf.Max(0, booster2StartCount);
        booster3Count = Mathf.Max(0, booster3StartCount);
        booster4Count = Mathf.Max(0, booster4StartCount);

        BindBoosterButtonsIfNeeded();
        ClearBoosterMode();
        UpdateBoosterUI();
    }
    private void BindBoosterButtonsIfNeeded()
    {
        if (!autoBindBoosterButtons) return;

        BindIfNoPersistent(booster1Button, OnClickBooster1_Blast3x3);
        BindIfNoPersistent(booster2Button, OnClickBooster2_CrossRowCol);
        BindIfNoPersistent(booster3Button, OnClickBooster3_FreeSwitch);
        BindIfNoPersistent(booster4Button, OnClickBooster4_RandomSpecial5);
    }

    private void BindIfNoPersistent(Button btn, System.Action handler)
    {
        if (btn == null || handler == null) return;

        // 인스펙터에 이미 연결(퍼시스턴트)이 있으면 건드리지 않음
        if (btn.onClick.GetPersistentEventCount() > 0) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => handler.Invoke());
    }

    private GameObject GetOrCreateBoosterTargetMark(ref GameObject inst)
    {
        if (boosterTargetMarkPrefab == null) return null;
        if (inst == null) inst = Instantiate(boosterTargetMarkPrefab);
        return inst;
    }

    private void HideBoosterTargetMark(ref GameObject inst, ref Tween tween)
    {
        if (tween != null && tween.IsActive()) tween.Kill();
        tween = null;

        if (inst != null)
            inst.SetActive(false);
    }

    private void ShowBoosterTargetMarkOnGem(Gem gem, ref GameObject inst, ref Tween tween)
    {
        if (gem == null)
        {
            HideBoosterTargetMark(ref inst, ref tween);
            return;
        }

        GameObject mark = GetOrCreateBoosterTargetMark(ref inst);
        if (mark == null) return;

        // 젬에 붙여서 같이 움직이게
        mark.transform.SetParent(gem.transform, false);
        mark.transform.localPosition = Vector3.zero;
        mark.transform.localRotation = Quaternion.identity;
        mark.transform.localScale = Vector3.one;
        mark.SetActive(true);

        // SortingOrder를 젬보다 위로 올림 (SpriteRenderer 방식)
        SpriteRenderer gemSr = gem.GetComponent<SpriteRenderer>();
        SpriteRenderer markSr = mark.GetComponent<SpriteRenderer>();
        if (gemSr != null && markSr != null)
        {
            markSr.sortingLayerID = gemSr.sortingLayerID;
            markSr.sortingOrder = gemSr.sortingOrder + boosterTargetSortingOrderOffset;
        }

        // 알파를 컨트롤할 대상 찾기 (SpriteRenderer 우선)
        if (tween != null && tween.IsActive()) tween.Kill();
        tween = null;

        float d = Mathf.Max(0.05f, boosterTargetPulseDuration);

        Sequence seq = DOTween.Sequence();

        // 스케일 펄스
        seq.Append(mark.transform.DOScale(boosterTargetPulseScale, d).SetEase(Ease.InOutSine));
        seq.Append(mark.transform.DOScale(1f, d).SetEase(Ease.InOutSine));

        // 알파 펄스(가능한 컴포넌트에만)
        if (markSr != null)
        {
            seq.Insert(0f, markSr.DOFade(boosterTargetAlphaMin, d).SetEase(Ease.InOutSine));
            seq.Insert(d, markSr.DOFade(1f, d).SetEase(Ease.InOutSine));
        }
        else
        {
            // 혹시 UI Image/CanvasGroup로 만들었을 경우 대응
            var img = mark.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                seq.Insert(0f, img.DOFade(boosterTargetAlphaMin, d).SetEase(Ease.InOutSine));
                seq.Insert(d, img.DOFade(1f, d).SetEase(Ease.InOutSine));
            }
            else
            {
                var cg = mark.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    seq.Insert(0f, cg.DOFade(boosterTargetAlphaMin, d).SetEase(Ease.InOutSine));
                    seq.Insert(d, cg.DOFade(1f, d).SetEase(Ease.InOutSine));
                }
            }
        }

        seq.SetLoops(-1, LoopType.Restart);
        tween = seq;
    }
    private void ConfirmAndHideBoosterTargetMark(GameObject inst, ref Tween loopTween)
    {
        if (loopTween != null && loopTween.IsActive()) loopTween.Kill();
        loopTween = null;

        if (inst == null) return;

        GameObject instLocal = inst; //  람다에서 ref 대신 로컬 참조만 사용

        instLocal.SetActive(true);

        float d = Mathf.Max(0.05f, boosterTargetConfirmDuration);

        Transform t = instLocal.transform;
        t.localScale = Vector3.one;

        SpriteRenderer sr = instLocal.GetComponent<SpriteRenderer>();
        var img = instLocal.GetComponent<UnityEngine.UI.Image>();
        var cg = instLocal.GetComponent<CanvasGroup>();

        // 알파 1로 리셋
        if (sr != null) { var c = sr.color; c.a = 1f; sr.color = c; }
        if (img != null) { var c = img.color; c.a = 1f; img.color = c; }
        if (cg != null) cg.alpha = 1f;

        Sequence seq = DOTween.Sequence();
        seq.Append(t.DOScale(boosterTargetConfirmScale, d).SetEase(Ease.OutBack));

        if (sr != null) seq.Join(sr.DOFade(0f, d).SetEase(Ease.OutSine));
        else if (img != null) seq.Join(img.DOFade(0f, d).SetEase(Ease.OutSine));
        else if (cg != null) seq.Join(cg.DOFade(0f, d).SetEase(Ease.OutSine));

        seq.OnComplete(() =>
        {
            if (instLocal == null) return;

            // 다음 표시를 위해 복구
            if (sr != null) { var c = sr.color; c.a = 1f; sr.color = c; }
            if (img != null) { var c = img.color; c.a = 1f; img.color = c; }
            if (cg != null) cg.alpha = 1f;

            instLocal.SetActive(false);
            t.localScale = Vector3.one;
        });

        loopTween = seq;
    }


    private void ToggleBooster(InGameBoosterType t)
    {
        if (!enableBoosters) return;
        if (isGameOver || isShuffling || isAnimating) return;

        if (t == InGameBoosterType.Blast3x3 && booster1Count <= 0) return;
        if (t == InGameBoosterType.CrossRowCol && booster2Count <= 0) return;
        if (t == InGameBoosterType.FreeSwitch && booster3Count <= 0) return;

        // 같은 부스터를 다시 누르면 "취소"
        if (activeBooster == t)
        {
            ClearBoosterMode();
            UpdateBoosterUI();
            return;
        }

        // 다른 부스터 선택 = 모드 전환
        ClearBoosterMode();
        activeBooster = t;

        // 일반 선택 제거
        if (selectedGem != null)
        {
            selectedGem.SetSelected(false);
            selectedGem = null;
        }

        ClearHint();
        idleTimer = 0f;

        if (boosterSelectClip != null) PlaySfx(boosterSelectClip);

        UpdateBoosterUI();
    }

    private void SetSelectedMark(GameObject mark, ref Tween tween, bool on)
    {
        if (tween != null && tween.IsActive()) tween.Kill();
        tween = null;

        if (mark == null) return;

        mark.SetActive(on);
        if (!on) return;

        Transform t = mark.transform;
        t.localScale = Vector3.one;

        // 알파 펄스 대상 찾기 (Image 우선, 없으면 CanvasGroup)
        UnityEngine.UI.Image img = mark.GetComponent<UnityEngine.UI.Image>();
        CanvasGroup cg = (img == null) ? mark.GetComponent<CanvasGroup>() : null;

        // 초기 알파 1
        if (img != null)
        {
            Color c = img.color; c.a = 1f; img.color = c;
        }
        else if (cg != null)
        {
            cg.alpha = 1f;
        }

        Sequence seq = DOTween.Sequence();

        // 스케일 펄스
        seq.Append(t.DOScale(boosterMarkPulseScale, boosterMarkPulseDuration).SetEase(Ease.InOutSine));
        seq.Append(t.DOScale(1f, boosterMarkPulseDuration).SetEase(Ease.InOutSine));

        // 알파 펄스
        if (img != null)
        {
            seq.Insert(0f, img.DOFade(boosterMarkAlphaMin, boosterMarkPulseDuration).SetEase(Ease.InOutSine));
            seq.Insert(boosterMarkPulseDuration, img.DOFade(1f, boosterMarkPulseDuration).SetEase(Ease.InOutSine));
        }
        else if (cg != null)
        {
            seq.Insert(0f, cg.DOFade(boosterMarkAlphaMin, boosterMarkPulseDuration).SetEase(Ease.InOutSine));
            seq.Insert(boosterMarkPulseDuration, cg.DOFade(1f, boosterMarkPulseDuration).SetEase(Ease.InOutSine));
        }

        seq.SetLoops(-1, LoopType.Restart);
        tween = seq;
    }


    private void ClearBoosterMode()
    {
        activeBooster = InGameBoosterType.None;

        // FreeSwitch 픽 정리
        if (freeSwitchFirstPick != null)
        {
            freeSwitchFirstPick.SetSelected(false);
            freeSwitchFirstPick = null;
        }

        if (freeSwitchSecondPick != null)
        {
            freeSwitchSecondPick.SetSelected(false);
            freeSwitchSecondPick = null;
        }

        // Booster1/2 타겟 정리
        if (boosterTargetPick != null)
        {
            boosterTargetPick.SetSelected(false);
            boosterTargetPick = null;
        }

        // 레거시/혼용 변수 안전 정리
        boosterTargetGem = null;

        // 타겟 링(프리팹) 정리
        HideBoosterTargetMark(ref boosterTargetMarkA, ref boosterTargetTweenA);
        HideBoosterTargetMark(ref boosterTargetMarkB, ref boosterTargetTweenB);

        // 슬롯 선택 마크 정리
        if (booster1SelectedMark != null) booster1SelectedMark.SetActive(false);
        if (booster2SelectedMark != null) booster2SelectedMark.SetActive(false);
        if (booster3SelectedMark != null) booster3SelectedMark.SetActive(false);

        // Booster4는 즉시 사용형이라 여기서 다룰 필요 없음(마크는 UpdateBoosterUI에서 꺼짐)
        if (booster1MarkTween != null && booster1MarkTween.IsActive()) booster1MarkTween.Kill();
        if (booster2MarkTween != null && booster2MarkTween.IsActive()) booster2MarkTween.Kill();
        if (booster3MarkTween != null && booster3MarkTween.IsActive()) booster3MarkTween.Kill();

        booster1MarkTween = null;
        booster2MarkTween = null;
        booster3MarkTween = null;

    }



    private void SetSelectedMark(GameObject mark, bool on, ref Tween tween)
    {
        if (mark == null) return;

        RectTransform rt = mark.GetComponent<RectTransform>();

        if (!on)
        {
            if (tween != null && tween.IsActive()) tween.Kill();
            tween = null;

            if (rt != null) rt.localScale = Vector3.one;
            mark.SetActive(false);
            return;
        }

        mark.SetActive(true);

        if (tween != null && tween.IsActive()) tween.Kill();
        tween = null;

        if (rt != null)
        {
            rt.localScale = Vector3.one;
            tween = rt.DOScale(boosterSelectedPulseScale, boosterSelectedPulseDuration)
                      .SetLoops(-1, LoopType.Yoyo)
                      .SetEase(boosterSelectedPulseEase);
        }
    }

    private void UpdateBoosterUI()
    {
        if (booster1CountText != null) booster1CountText.text = $"x{booster1Count}";
        if (booster2CountText != null) booster2CountText.text = $"x{booster2Count}";
        if (booster3CountText != null) booster3CountText.text = $"x{booster3Count}";
        if (booster4CountText != null) booster4CountText.text = $"x{booster4Count}";

        bool lockUI = isAnimating || isGameOver || isShuffling;

        if (booster1Button != null) booster1Button.interactable = !lockUI && booster1Count > 0;
        if (booster2Button != null) booster2Button.interactable = !lockUI && booster2Count > 0;
        if (booster3Button != null) booster3Button.interactable = !lockUI && booster3Count > 0;
        if (booster4Button != null) booster4Button.interactable = !lockUI && booster4Count > 0;

        SetSelectedMark(booster1SelectedMark, activeBooster == InGameBoosterType.Blast3x3, ref booster1MarkTween);
        SetSelectedMark(booster2SelectedMark, activeBooster == InGameBoosterType.CrossRowCol, ref booster2MarkTween);
        SetSelectedMark(booster3SelectedMark, activeBooster == InGameBoosterType.FreeSwitch, ref booster3MarkTween);

        // Booster4는 "모드"가 아니라 즉시 사용형이라 기본은 꺼둬도 됨.
        // 원하면: 누르는 순간만 짧게 켜는 플래시 연출을 OnClickBooster4에서 처리(아래 3번에 포함).
        SetSelectedMark(booster4SelectedMark, false, ref booster4MarkTween);
        // 선택 마크(SelectedMark) - activeBooster만 펄스
        SetSelectedMark(booster1SelectedMark, ref booster1MarkTween, activeBooster == InGameBoosterType.Blast3x3);
        SetSelectedMark(booster2SelectedMark, ref booster2MarkTween, activeBooster == InGameBoosterType.CrossRowCol);
        SetSelectedMark(booster3SelectedMark, ref booster3MarkTween, activeBooster == InGameBoosterType.FreeSwitch);
        // booster4는 즉시형이면 선택 마크 유지 안 하는 편이 깔끔(원하면 여기서도 on 처리 가능)

    }


    // ===== Boosters: Click Handling =====
    void HandleBoosterClick(Gem gem)
    {
        if (gem == null) return;
        if (isGameOver || isShuffling || isAnimating) return;

        ClearHint();
        idleTimer = 0f;

        // ICE 대상 제외(룰 유지)
        if (IsIce(gem.x, gem.y)) return;

        switch (activeBooster)
        {
            case InGameBoosterType.Blast3x3:
                {
                    if (booster1Count <= 0) { ClearBoosterMode(); UpdateBoosterUI(); return; }

                    // 1클릭: 타겟 지정(링 표시)
                    if (boosterTargetPick == null)
                    {
                        boosterTargetPick = gem;
                        ShowBoosterTargetMarkOnGem(boosterTargetPick, ref boosterTargetMarkA, ref boosterTargetTweenA);
                        HideBoosterTargetMark(ref boosterTargetMarkB, ref boosterTargetTweenB);
                        return;
                    }

                    // 다른 젬 클릭: 타겟 변경(링 이동)
                    if (boosterTargetPick != gem)
                    {
                        boosterTargetPick = gem;
                        ShowBoosterTargetMarkOnGem(boosterTargetPick, ref boosterTargetMarkA, ref boosterTargetTweenA);
                        return;
                    }

                    // 같은 젬 2클릭: 발동
                    int bx = gem.x;
                    int by = gem.y;

                    boosterTargetPick = null;
                    ConfirmAndHideBoosterTargetMark(boosterTargetMarkA, ref boosterTargetTweenA);

                    StartCoroutine(UseBoosterMaskRoutine(Make3x3Mask(bx, by), InGameBoosterType.Blast3x3));
                    return;
                }

            case InGameBoosterType.CrossRowCol:
                {
                    if (booster2Count <= 0) { ClearBoosterMode(); UpdateBoosterUI(); return; }

                    // 1클릭: 타겟 지정(링 표시)
                    if (boosterTargetPick == null)
                    {
                        boosterTargetPick = gem;
                        ShowBoosterTargetMarkOnGem(boosterTargetPick, ref boosterTargetMarkA, ref boosterTargetTweenA);
                        HideBoosterTargetMark(ref boosterTargetMarkB, ref boosterTargetTweenB);
                        return;
                    }

                    // 다른 젬 클릭: 타겟 변경(링 이동)
                    if (boosterTargetPick != gem)
                    {
                        boosterTargetPick = gem;
                        ShowBoosterTargetMarkOnGem(boosterTargetPick, ref boosterTargetMarkA, ref boosterTargetTweenA);
                        return;
                    }

                    // 같은 젬 2클릭: 발동
                    int cx = gem.x;
                    int cy = gem.y;

                    boosterTargetPick = null;
                    ConfirmAndHideBoosterTargetMark(boosterTargetMarkA, ref boosterTargetTweenA);

                    StartCoroutine(UseBoosterMaskRoutine(MakeCrossRowColMask(cx, cy), InGameBoosterType.CrossRowCol));
                    return;
                }

            case InGameBoosterType.FreeSwitch:
                {
                    if (booster3Count <= 0) { ClearBoosterMode(); UpdateBoosterUI(); return; }

                    // 1픽: A젬 선택
                    if (freeSwitchFirstPick == null)
                    {
                        freeSwitchFirstPick = gem;
                        freeSwitchFirstPick.SetSelected(true);

                        ShowBoosterTargetMarkOnGem(freeSwitchFirstPick, ref boosterTargetMarkA, ref boosterTargetTweenA);
                        HideBoosterTargetMark(ref boosterTargetMarkB, ref boosterTargetTweenB);
                        return;
                    }

                    // A젬을 다시 누르면 취소
                    if (freeSwitchFirstPick == gem)
                    {
                        freeSwitchFirstPick.SetSelected(false);
                        freeSwitchFirstPick = null;

                        freeSwitchSecondPick = null;

                        HideBoosterTargetMark(ref boosterTargetMarkA, ref boosterTargetTweenA);
                        HideBoosterTargetMark(ref boosterTargetMarkB, ref boosterTargetTweenB);

                        return;
                    }

                    // 2픽: B젬 선택 즉시 교환 발동
                    freeSwitchSecondPick = gem;
                    freeSwitchSecondPick.SetSelected(true);

                    ShowBoosterTargetMarkOnGem(freeSwitchFirstPick, ref boosterTargetMarkA, ref boosterTargetTweenA);
                    ShowBoosterTargetMarkOnGem(freeSwitchSecondPick, ref boosterTargetMarkB, ref boosterTargetTweenB);

                    StartCoroutine(UseBooster3_FreeSwitchRoutine(freeSwitchFirstPick, freeSwitchSecondPick));
                    return;
                }
        }
    }



    // ===== Boosters: Masks =====
    private bool[,] Make3x3Mask(int cx, int cy)
    {
        bool[,] mask = new bool[width, height];

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

        return mask;
    }

    private bool[,] MakeCrossRowColMask(int x, int y)
    {
        bool[,] mask = new bool[width, height];

        if (y >= 0 && y < height)
        {
            for (int xx = 0; xx < width; xx++)
                mask[xx, y] = true;
        }

        if (x >= 0 && x < width)
        {
            for (int yy = 0; yy < height; yy++)
                mask[x, yy] = true;
        }

        return mask;
    }

    // ===== Boosters: Execution =====
    private IEnumerator UseBoosterMaskRoutine(bool[,] mask, InGameBoosterType used)
    {
        if (isAnimating) yield break;
        if (mask == null) yield break;

        isAnimating = true;
        BeginIceHitWindow();

        if (selectedGem != null)
        {
            selectedGem.SetSelected(false);
            selectedGem = null;
        }

        if (boosterUseClip != null) PlaySfx(boosterUseClip);

        // 1) 클리어
        int cleared = ClearByMaskWithChain(mask);
        if (cleared > 0)
            AddScoreForClear(cleared, comboMultiplier: 1);

        // 2) 부스터 카운트 차감
        if (used == InGameBoosterType.Blast3x3) booster1Count--;
        if (used == InGameBoosterType.CrossRowCol) booster2Count--;

        // 3) Move 소모(옵션)
        if (used == InGameBoosterType.Blast3x3 && booster1ConsumesMove)
        {
            movesLeft--;
            UpdateMovesUI();
        }
        if (used == InGameBoosterType.CrossRowCol && booster2ConsumesMove)
        {
            movesLeft--;
            UpdateMovesUI();
        }

        UpdateBoosterUI();

        // 4) 승/패 체크(승리 시 클리어 보너스 루틴 포함)
        if (IsStageCleared())
        {
            yield return StartCoroutine(StageClearBonusRoutine());
            isAnimating = false;
            yield break;
        }
        if (movesLeft <= 0)
        {
            EndGame(false);
            isAnimating = false;
            yield break;
        }

        // 5) 리필 + 캐스케이드
        if (cleared > 0)
        {
            yield return new WaitForSeconds(popDuration);
            yield return StartCoroutine(PostClearRefillAndEnsure());

            int combo = 0;
            while (true)
            {
                int c = CheckMatchesAndClear_WithPromotionsSafe();
                if (c <= 0) break;

                combo++;
                AddScoreForClear(c, comboMultiplier: combo);

                if (IsStageCleared())
                {
                    yield return StartCoroutine(StageClearBonusRoutine());
                    isAnimating = false;
                    yield break;
                }

                yield return new WaitForSeconds(popDuration);
                yield return StartCoroutine(PostClearRefillAndEnsure());
            }
        }

        // 6) 막힘이면 셔플
        if (!HasAnyPossibleMove())
            yield return ShuffleRoutine(force: true);

        isAnimating = false;      // 먼저 잠금 해제
        ClearBoosterMode();
        UpdateBoosterUI();        // 잠금 해제된 상태로 UI 갱신

    }

    private IEnumerator UseBooster3_FreeSwitchRoutine(Gem first, Gem second)
    {
        if (isAnimating) yield break;
        if (first == null || second == null) yield break;

        // ICE는 Swap 불가(최종 차단은 SwapGems에서도 수행)
        if (IsIce(first.x, first.y) || IsIce(second.x, second.y))
        {
            ClearBoosterMode();
            UpdateBoosterUI();
            yield break;
        }

        isAnimating = true;
        BeginIceHitWindow();

        if (selectedGem != null)
        {
            selectedGem.SetSelected(false);
            selectedGem = null;
        }

        if (boosterUseClip != null) PlaySfx(boosterUseClip);

        //  손맛 연출: 링 펄스 후 사라짐 + 젬 ‘툭’(스케일 펀치)
        ConfirmAndHideBoosterTargetMark(boosterTargetMarkA, ref boosterTargetTweenA);
        ConfirmAndHideBoosterTargetMark(boosterTargetMarkB, ref boosterTargetTweenB);

        if (first != null) first.transform.DOPunchScale(Vector3.one * boosterSwapPunchScale, boosterSwapPunchDuration, 8, 0.8f);
        if (second != null) second.transform.DOPunchScale(Vector3.one * boosterSwapPunchScale, boosterSwapPunchDuration, 8, 0.8f);

        yield return new WaitForSeconds(boosterSwapPunchDuration);

        // 1) 스왑(매치 없어도 되돌리지 않음)
        SwapGems(first, second);
        yield return new WaitForSeconds(swapResolveDelay);

        // 2) ColorBomb + Stripe 조합(기존 루틴 재사용)
        if (IsColorStripeCombo(first, second))
        {
            Gem colorBomb = first.IsColorBomb ? first : second;
            Gem stripe = (colorBomb == first) ? second : first;

            yield return StartCoroutine(ColorBombStripeComboRoutine(colorBomb, stripe));

        }
        else
        {
            bool anyColor = first.IsColorBomb || second.IsColorBomb;
            bool anyWrapped = first.IsWrappedBomb || second.IsWrappedBomb;
            bool anyStripe = first.IsRowBomb || first.IsColBomb || second.IsRowBomb || second.IsColBomb;

            // 프로젝트 룰: Stripe 즉발(컬러/랩드 혼합은 제외)
            if (!anyColor && anyStripe && !anyWrapped)
            {
                int cleared = ResolveStripeImmediate(first, second);
                if (cleared > 0)
                {
                    AddScoreForClear(cleared, comboMultiplier: 1);


                    yield return new WaitForSeconds(popDuration);
                    yield return StartCoroutine(PostClearRefillAndEnsure());
                }
            }
            // Wrapped + Normal 즉발
            else if (!anyColor && IsWrappedNormalSwap(first, second))
            {
                Gem wrapped = first.IsWrappedBomb ? first : second;
                int cleared = ActivateWrapped(wrapped);

                if (cleared > 0)
                {
                    AddScoreForClear(cleared, comboMultiplier: 1);


                    yield return new WaitForSeconds(popDuration);
                    yield return StartCoroutine(PostClearRefillAndEnsure());
                }
            }
            else
            {
                // 나머지 특수 조합 처리
                int specialCleared = ResolveSpecialSwapIfNeeded(first, second);
                if (specialCleared > 0)
                {
                    AddScoreForClear(specialCleared, comboMultiplier: 1);


                    yield return new WaitForSeconds(popDuration);
                    yield return StartCoroutine(PostClearRefillAndEnsure());
                }
            }

            // 3) 매치/캐스케이드
            int combo = 0;
            while (true)
            {
                int c = CheckMatchesAndClear_WithPromotionsSafe();
                if (c <= 0) break;

                combo++;
                AddScoreForClear(c, comboMultiplier: combo);


                if (IsStageCleared())
                {
                    yield return StartCoroutine(StageClearBonusRoutine());
                    isAnimating = false;
                    yield break;
                }

                yield return new WaitForSeconds(popDuration);
                yield return StartCoroutine(PostClearRefillAndEnsure());
            }
        }

        // 4) 부스터 카운트/Move 소모(옵션)
        booster3Count--;

        if (booster3ConsumesMove)
        {
            movesLeft--;
            UpdateMovesUI();
        }

        UpdateBoosterUI();

        // 5) 승/패 체크
        if (IsStageCleared())
        {
            yield return StartCoroutine(StageClearBonusRoutine());
            isAnimating = false;
            yield break;
        }
        if (movesLeft <= 0)
        {
            EndGame(false);
            isAnimating = false;
            yield break;
        }

        // 6) 막힘이면 셔플
        if (!HasAnyPossibleMove())
            yield return ShuffleRoutine(force: true);

        isAnimating = false;
        ClearBoosterMode();
        UpdateBoosterUI();

    }

    private IEnumerator UseBooster4_RandomSpecial5Routine()
{
    if (isAnimating) yield break;

    isAnimating = true;

    // 일반 선택 제거
    if (selectedGem != null)
    {
        selectedGem.SetSelected(false);
        selectedGem = null;
    }

    // 부스터 모드도 정리(겹침 방지)
    ClearBoosterMode();

    ClearHint();
    idleTimer = 0f;

    if (boosterUseClip != null) PlaySfx(boosterUseClip);

        List<Gem> picks = PickRandomNormalGemsForBooster4(5);
        if (picks == null || picks.Count == 0)
        {
            // 변환할 대상이 없으면 부스터 소모/턴 소모 없이 종료
            isAnimating = false;
            UpdateBoosterUI();
            yield break;
        }

        // 기준 스케일(절대값 1 금지)
        Vector3 baseScale = gemBaseScale;

        // 1) 프리 연출: 5개가 "동시에" 살짝 떨림(펀치)
        Vector3 punchDelta = new Vector3(baseScale.x * booster4PrePunch, baseScale.y * booster4PrePunch, 0f);
        for (int i = 0; i < picks.Count; i++)
        {
            Gem g = picks[i];
            if (g == null) continue;

            Transform t = g.transform;
            t.DOKill(true);
            t.localScale = baseScale;

            t.DOPunchScale(punchDelta, booster4PrePunchDuration, vibrato: 10, elasticity: 0.8f);
        }

        yield return new WaitForSeconds(booster4PrePunchDuration);

        // 2) 순차 변환: (축소로 사라지는 맛) -> SetSpecial -> (팝업 등장) -> 원복
        for (int i = 0; i < picks.Count; i++)
        {
            Gem g = picks[i];
            if (g == null) continue;

            Transform t = g.transform;
            t.DOKill(true);
            t.localScale = baseScale;

            // 살짝 사라짐(축소)
            yield return t.DOScale(baseScale * booster4VanishScale, booster4VanishDuration)
                          .SetEase(Ease.InBack)
                          .WaitForCompletion();

            // 특수젬으로 변환
            SpecialGemType st = PickBooster4SpecialType();
            g.SetSpecial(st);

            // 변환 직후 스케일 잔류 방지(절대 1 금지)
            t.DOKill(true);
            t.localScale = baseScale * booster4VanishScale;

            // 등장 팝(확대 후 원복)
            yield return t.DOScale(baseScale * booster4PostPopScale, booster4PostPopDuration)
                          .SetEase(Ease.OutQuad)
                          .WaitForCompletion();

            yield return t.DOScale(baseScale, booster4PostPopDuration)
                          .SetEase(Ease.OutQuad)
                          .WaitForCompletion();

            // 다음 변환까지 템포
            if (booster4StaggerDelay > 0f)
                yield return new WaitForSeconds(booster4StaggerDelay);
        }


        booster4Count--;

    if (booster4ConsumesMove)
    {
        movesLeft--;
        UpdateMovesUI();
    }

    UpdateBoosterUI();

    // 변환 후 매치가 생기면 정리
    int combo = 0;
    while (true)
    {
        int c = CheckMatchesAndClear_WithPromotionsSafe();
        if (c <= 0) break;

        combo++;
        AddScoreForClear(c, comboMultiplier: combo);

        if (IsStageCleared())
        {
            yield return StartCoroutine(StageClearBonusRoutine());
            isAnimating = false;
            yield break;
        }

        yield return new WaitForSeconds(popDuration);
        yield return StartCoroutine(PostClearRefillAndEnsure());
    }

    if (IsStageCleared())
    {
        yield return StartCoroutine(StageClearBonusRoutine());
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
        yield return ShuffleRoutine(force: true);

    isAnimating = false;
    UpdateBoosterUI();
}

    private List<Gem> PickRandomNormalGemsForBooster4(int count)
    {
        List<Gem> candidates = new List<Gem>(width * height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Gem g = gems[x, y];
                if (g == null) continue;
                if (IsIce(x, y)) continue;
                if (g.IsSpecial) continue;
                if (g.IsColorBomb) continue;

                candidates.Add(g);
            }
        }

        List<Gem> picks = new List<Gem>();
        if (candidates.Count == 0) return picks;

        int target = Mathf.Min(count, candidates.Count);
        for (int i = 0; i < target; i++)
        {
            int idx = Random.Range(0, candidates.Count);
            picks.Add(candidates[idx]);
            candidates.RemoveAt(idx);
        }

        return picks;
    }

    private int ConvertRandomNormalGemsToSpecial(int count)
    {
        if (count <= 0) return 0;

        List<Gem> candidates = new List<Gem>(width * height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Gem g = gems[x, y];
                if (g == null) continue;
                if (IsIce(x, y)) continue;          // ICE 안은 변환 제외(룰 유지)
                if (g.IsSpecial) continue;          // 이미 특수면 제외
                if (g.IsColorBomb) continue;        // 컬러밤은 별도 정책(기본 제외)
                candidates.Add(g);
            }
        }

        if (candidates.Count == 0) return 0;

        int converted = 0;
        int target = Mathf.Min(count, candidates.Count);

        for (int i = 0; i < target; i++)
        {
            int idx = Random.Range(0, candidates.Count);
            Gem pick = candidates[idx];
            candidates.RemoveAt(idx);

            if (pick == null) continue;

            SpecialGemType st = PickBooster4SpecialType();
            pick.SetSpecial(st);

            // 변환 후 스케일/색 상태 확실히 원복
            pick.ResetVisual(killMoveTween: true);

            converted++;

        }

        return converted;
    }

    private SpecialGemType PickBooster4SpecialType()
    {
        // 기본: Row/Col 위주, 옵션으로 Wrapped/ColorBomb 허용
        float r = Random.value;

        if (booster4AllowColorBomb && r < 0.05f)
            return SpecialGemType.ColorBomb;

        if (booster4AllowWrapped && r < 0.20f)
            return SpecialGemType.WrappedBomb;

        return (Random.value < 0.5f) ? SpecialGemType.RowBomb : SpecialGemType.ColBomb;
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

        if (IsStageCleared())
        {
            yield return StartCoroutine(StageClearBonusRoutine());
            yield break;
        }

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
    void RegisterGemCollectedIfNeeded(Gem g)
    {
        if (g == null) return;

        bool needCollect =
            (levelGoalType == LevelGoalType.CollectColor ||
             levelGoalType == LevelGoalType.ScoreAndCollectColor ||
             levelGoalType == LevelGoalType.CollectMultiColor ||
             levelGoalType == LevelGoalType.ClearAllIceAndCollectMultiColor);

        if (!needCollect) return;

        // 1) 기존 단일 색 Collect 호환
        if (levelGoalType == LevelGoalType.CollectColor || levelGoalType == LevelGoalType.ScoreAndCollectColor)
        {
            if (g.type == collectGemType)
            {
                collectedCount++;
                UpdateGoalTextUI();
                UpdateGoalGaugeUI();
                NotifyGoalProgressChanged();
            }
            return;
        }

        // 2) 멀티 Collect
        if (collectGemTypesMulti == null || collectTargetsMulti == null || collectedMulti == null) return;

        for (int i = 0; i < collectGemTypesMulti.Length; i++)
        {
            if (g.type != collectGemTypesMulti[i]) continue;

            collectedMulti[i]++;
            UpdateGoalTextUI();
            UpdateGoalGaugeUI();
            NotifyGoalProgressChanged();
            return;
        }
    }


    private void RegisterIceDestroyed()
    {
        clearedIce++;
        UpdateGoalTextUI();
        UpdateGoalGaugeUI();
        NotifyGoalProgressChanged();
    }
    private void AddFlatScore(int points)
    {
        if (points <= 0) return;

        score += points;
        UpdateScoreUI();

    }

    private void AddScoreForClear(int clearedCount, int comboMultiplier)
    {
        if (clearedCount <= 0) return;

        int gained = baseScorePerGem * clearedCount * Mathf.Max(1, comboMultiplier);
        score += gained;

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

            float delay = 0.01f * op.toY;

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

        float delay = 0.01f * toY;

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
    private IEnumerator StageClearBonusRoutine()
    {
        // 입력/힌트 차단
        isAnimating = true;
        ClearHint();
        selectedGem = null;

        int startScore = score;
        int safety = 0;

        if (!enableStageClearMoveBonus || movesLeft <= 0)
        {
            lastClearBonusScore = 0;
            EndGame(true);
            yield break;
        }

        while (movesLeft > 0 && safety++ < 999)
        {
            // 1) 남은 무브 소모
            movesLeft--;
            UpdateMovesUI();

            // 2) 보너스로 터뜨릴 타겟 선택: 남아있는 특수젬 우선
            if (!TryPickAnySpecialGem(out int bx, out int by))
            {
                // 특수젬이 없다면 일반 젬 랜덤 선택
                if (!TryPickRandomBonusGem(out bx, out by))
                    break;
            }

            Gem g = gems[bx, by];
            if (g == null) continue;

            // (옵션) 보너스 중에는 ICE를 무시하고 터뜨리기 원하면, 먼저 ICE 제거
            if (stageClearBonusIgnoreIceForDetonation && IsIce(bx, by))
            {
                ForceRemoveIceForBonus(bx, by);
            }

            // 3) 선택된 젬이 일반젬이면 일정 확률로 특수젬으로 변환
            if (!g.IsSpecial && Random.Range(0, 100) < stageClearBonusMakeSpecialChance)
            {
                g.SetSpecial(PickRandomBonusSpecialType());
            }


            // 4) 해당 칸을 트리거로 체인 클리어
            bool[,] mask = new bool[width, height];
            mask[bx, by] = true;

            int cleared = ClearByMaskWithChain(mask);
            AddScoreForClear(cleared, comboMultiplier: 1);

            // 5) 터짐/리필/캐스케이드 정리
            yield return new WaitForSeconds(popDuration);
            yield return StartCoroutine(PostClearRefillAndEnsure());

            if (stageClearBonusStepDelay > 0f)
                yield return new WaitForSeconds(stageClearBonusStepDelay);
            if (stageClearBonusAlsoDetonateRemainingSpecials)
            {
                yield return StartCoroutine(DetonateAllRemainingSpecialsRoutine());
            }
        }

        lastClearBonusScore = score - startScore;

        EndGame(true);
    }
    private IEnumerator DetonateAllRemainingSpecialsRoutine()
    {
        int safety = 0;

        while (safety++ < 999)
        {
            if (!TryPickAnySpecialGem(out int x, out int y))
                yield break;

            if (stageClearBonusIgnoreIceForDetonation && IsIce(x, y))
            {
                ForceRemoveIceForBonus(x, y);
            }

            bool[,] mask = new bool[width, height];
            mask[x, y] = true;

            int cleared = ClearByMaskWithChain(mask);
            AddScoreForClear(cleared, comboMultiplier: 1);

            yield return new WaitForSeconds(popDuration);
            yield return StartCoroutine(PostClearRefillAndEnsure());

            if (stageClearBonusStepDelay > 0f)
                yield return new WaitForSeconds(stageClearBonusStepDelay);
        }
    }
    private bool TryPickAnySpecialGem(out int outX, out int outY)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Gem g = gems[x, y];
                if (g == null) continue;
                if (!g.IsSpecial) continue;

                // 보너스에서 ICE를 무시하지 않을 거면 ICE 안 특수젬은 건드리지 않음
                if (!stageClearBonusIgnoreIceForDetonation && IsIce(x, y))
                    continue;

                outX = x;
                outY = y;
                return true;
            }
        }

        outX = -1;
        outY = -1;
        return false;
    }
    private void ForceRemoveIceForBonus(int x, int y)
    {
        if (obstacles == null) return;
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        if (obstacles[x, y] != ObstacleType.Ice) return;

        obstacles[x, y] = ObstacleType.None;

        if (iceHp != null) iceHp[x, y] = 0;

        if (iceObjects != null && iceObjects[x, y] != null)
        {
            Destroy(iceObjects[x, y]);
            iceObjects[x, y] = null;
        }
    }

    private bool TryPickRandomBonusGem(out int outX, out int outY)
    {
        // 랜덤 1차 시도
        for (int i = 0; i < 40; i++)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            if (IsIce(x, y)) continue;
            if (gems[x, y] == null) continue;

            outX = x;
            outY = y;
            return true;
        }

        // 폴백: 스캔
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (IsIce(x, y)) continue;
                if (gems[x, y] == null) continue;

                outX = x;
                outY = y;
                return true;
            }
        }

        outX = -1;
        outY = -1;
        return false;
    }

    private SpecialGemType PickRandomBonusSpecialType()
    {
        // Row/Col 중심 + (옵션) Wrapped 소량
        float r = Random.value;

        if (stageClearBonusAllowWrapped && r < 0.15f)
            return SpecialGemType.WrappedBomb;

        return (r < 0.575f) ? SpecialGemType.RowBomb : SpecialGemType.ColBomb;
    }
    [Header("Result Popup (Art UI)")]
    [SerializeField] private ResultPopupUi resultPopupUi; // GameOverPanel에 붙인 ResultPopupUi 연결

    private void EndGame(bool isWin)
    {
        

        isGameOver = true;

        bool hasNextStage = (StageManager.Instance != null &&
                             StageManager.Instance.HasNextStage());
        Debug.Log($"[EndGame] isWin={isWin}, score={score}, passScore={passScore}, movesLeft={movesLeft}, maxMoves={maxMoves}, hasNextStage={hasNextStage}, resultPopupUi={(resultPopupUi ? resultPopupUi.name : "NULL")}");


        ClearHint();

        if (gameOverPanel != null)
            // ===== Art Result Popup UI =====
            if (resultPopupUi != null)
            {
                int earnedStars = 0;
                if (isWin)
                {
                    if (score >= passScore) earnedStars = 1;
                    if (score >= star2Score) earnedStars = 2;
                    if (score >= star3Score) earnedStars = 3;
                }

                int bonus = isWin ? Mathf.Max(0, lastClearBonusScore) : 0;
                int finalScore = score; // 네 프로젝트 기준: 이미 score에 반영된 구조면 그대로 사용

                resultPopupUi.Show(
          isWin,
          earnedStars,
          passScore,
          score,
          movesLeft,
          maxMoves,
          bonus,
          finalScore,
          hasNextStage
      );


                return; // 아래 "구형 텍스트 결과창" 로직은 건너뛴다.

            }

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
                if (!isWin) lastClearBonusScore = 0;
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
            int bonusScore = Mathf.Max(0, lastClearBonusScore);
            int finalScore = score;

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
        // BoardManager.cs / BoardManager / EndGame(bool isWin) 맨 아래에 추가
        if (resultPopupUi != null)
        {
            // 별 계산 (기존 퍼센트 기준)
            float p = (targetScore > 0) ? (float)score / targetScore : (isWin ? 1f : 0f);
            int earnedStars =
                (p >= star3Percent) ? 3 :
                (p >= star2Percent) ? 2 :
                (p >= star1Percent) ? 1 : 0;

            int usedMoves = maxMoves - movesLeft;
            int bonusScore = isWin ? Mathf.Max(0, lastClearBonusScore) : 0;
            int finalScore = score; // 너 프로젝트 기준: 보너스를 score에 합산 안 하면 그대로 score, 합산이면 score+bonusScore로 변경

            

            Debug.Log($"[EndGame->Popup] win={isWin}, stars={earnedStars}, target={targetScore}, score={score}, movesLeft={movesLeft}, maxMoves={maxMoves}, bonus={bonusScore}, final={finalScore}, hasNext={hasNextStage}");

            resultPopupUi.Show(
                isWin,
                earnedStars,
                targetScore,
                score,
                movesLeft,
                maxMoves,
                bonusScore,
                finalScore,
                hasNextStage
            );
        }
        else
        {
            Debug.LogWarning("[EndGame->Popup] resultPopupUi is NULL (Inspector reference missing).");
        }

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
        lastClearBonusScore = 0;
        movesLeft = maxMoves;

        UpdateScoreUI();
        UpdateMovesUI();
        HideComboBannerImmediate();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (shuffleOverlay != null) shuffleOverlay.SetActive(false);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (nextStageButton != null) nextStageButton.gameObject.SetActive(false);
        star1Shown = star2Shown = star3Shown = false;
        if (star1Image != null) star1Image.gameObject.SetActive(false);
        if (star2Image != null) star2Image.gameObject.SetActive(false);
        if (star3Image != null) star3Image.gameObject.SetActive(false);
        if (goalFillImage != null) goalFillImage.fillAmount = 0f;
    }
    private IEnumerator AlignBoardNextFrame()
    {
        yield return null;
        yield return null;
        AlignBoardToMiddleArea();
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
        SetupStarThresholds();
        CacheGemBaseScale();
        ResetState();
        InitBoostersForStage();


        // 2) 보드 생성
        gems = new Gem[width, height];
        GenerateBoard();
        StartCoroutine(AlignBoardNextFrame());


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
        // ICE 개수 재계산 + GoalType 적용(ICE 스테이지면 AND로 자동 전환)
        RecountTotalIce();
        ApplyGoalFromStageData(StageManager.Instance != null ? StageManager.Instance.CurrentStage : null);
        Debug.Log($"[Stage] id={StageManager.Instance?.CurrentStage?.stageID} useCollect={StageManager.Instance?.CurrentStage?.useCollectGoal} targets={(StageManager.Instance?.CurrentStage?.collectTargets == null ? 0 : StageManager.Instance.CurrentStage.collectTargets.Count)} totalIce={totalIce} goalType={levelGoalType}");


        // 런타임 진행도 리셋 + UI 갱신
        ResetGoalProgress();
        UpdateGoalTextUI();
        UpdateGoalGaugeUI();

        // 5) 시작 1회 보드 검증(즉시 매치 제거 + 무브 확보)
        StartCoroutine(ShuffleRoutine(force: false));

        // 6) UI
        UpdateScoreUI();
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
       
        // 최종 파괴 확정 시점 (obstacles None으로 내리기 전에든 후든 1회만 보장되면 OK)
        RegisterIceDestroyed();

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
            RegisterGemCollectedIfNeeded(colorBomb);
            gems[colorBomb.x, colorBomb.y] = null;
            Destroy(colorBomb.gameObject);
            totalCleared++;
        }


        if (stripe != null && gems[stripe.x, stripe.y] == stripe)
        {
            RegisterGemCollectedIfNeeded(stripe);
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

            if (IsStageCleared())
            {
                yield return StartCoroutine(StageClearBonusRoutine());
                yield break;
            }

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
                // 실제 삭제 확정 시점에서만 Collect 카운트
                RegisterGemCollectedIfNeeded(g);
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
        // [DEBUG] 결과창 바로 띄우기
        if (Input.GetKeyDown(KeyCode.F9)) { DebugShowResultPopup(true); return; } // 승리 팝업
        if (Input.GetKeyDown(KeyCode.F10)) { DebugShowResultPopup(false); return; } // 실패 팝업

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
    // [DEBUG] Force show result popup without playing the stage
    private void DebugShowResultPopup(bool isWin)
    {
        if (gameOverPanel == null)
        {
            Debug.LogWarning("[DEBUG] gameOverPanel is null. Assign it in BoardManager inspector.");
            return;
        }

        // 팝업 루트 활성화 (비활성 상태면 UI 스크립트가 동작 못함)
        gameOverPanel.SetActive(true);

        // 새 ResultPopupUi가 붙어있으면 그걸 우선 사용
        var popup = gameOverPanel.GetComponent<ResultPopupUi>();
        if (popup != null)
        {
            int stars = isWin ? 3 : 0;

            // 네 프로젝트 기준: 점수/타겟은 BoardManager 값 재사용 (없으면 적당히 수정)
            int targetScore = passScore;
            int currentScore = score;
            int finalScore = score; // 지금 구조상 finalScore를 따로 굴리면 그 변수로 교체

            bool hasNextStage = false;
            if (isWin && StageManager.Instance != null)
                hasNextStage = StageManager.Instance.HasNextStage();

            int bonus = isWin ? Mathf.Max(0, lastClearBonusScore) : 0;

            popup.Show(
                isWin: isWin,
                earnedStars: stars,      //  핵심: stars -> earnedStars
                targetScore: targetScore,
                score: currentScore,
                movesLeft: movesLeft,
                maxMoves: maxMoves,
                bonusScore: bonus,
                finalScore: finalScore,
                hasNextStage: hasNextStage
            );


            // 보드 입력 막기(원하면 유지)
            isGameOver = true;
            return;
        }

        // (fallback) 구 UI 로직만 남아있는 경우 대비
        Debug.LogWarning("[DEBUG] ResultPopupUi component not found on gameOverPanel. Showing legacy panel only.");
        isGameOver = true;
    }

    private void DebugHideResultPopup()
    {
        if (gameOverPanel == null) return;

        var popup = gameOverPanel.GetComponent<ResultPopupUi>();
        if (popup != null)
        {
            popup.HideImmediate();
        }
        else
        {
            gameOverPanel.SetActive(false);
        }

        // 다시 플레이 가능하게 풀기(원하면 제거)
        isGameOver = false;
    }

    #endregion
}
