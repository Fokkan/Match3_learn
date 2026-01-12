using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    public BoardManager board { get; private set; }
    public int currentStageIndex { get; private set; } = 0;

    public StageData CurrentStage { get; private set; }

    [Header("DB")]
    [SerializeField] private StageDatabase stageDB;

    [Header("Save Key")]
    [SerializeField] private string selectedStageKey = "SelectedStageId";
    [SerializeField] private int fallbackStageId = 1;

    [Header("Auto Load")]
    [SerializeField] private bool autoLoadOnStart = true;
    private static StageManager _instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (board == null)
            board = Object.FindFirstObjectByType<BoardManager>();
        DontDestroyOnLoad(gameObject);

        // Start()에서 하던 자동 로드를 Awake로 당긴다 (씬 로드 타이밍 문제 방지)
        if (autoLoadOnStart)
        {
            int stageId = PlayerPrefs.GetInt(selectedStageKey, fallbackStageId);
            LoadStageById(stageId);
        }

    }

    // StageManager.cs
    // 위치: private void Start()  <-- 이 함수 전체를 교체
    private void Start()
    {
        // Awake에서 이미 자동 로드를 처리함.
    }


    public bool LoadStageById(int stageId)
    {
        if (stageDB == null)
        {
            Debug.LogError("[StageManager] StageDatabase reference missing.");
            return false;
        }

        StageData stage = stageDB.GetStageById(stageId);
        if (stage == null)
        {
            Debug.LogError($"[StageManager] StageData not found for stageId={stageId}. (DB Count={stageDB.Count})");
            return false;
        }

        CurrentStage = stage;
        int idx = stageDB.GetIndexById(stage.stageID);
        currentStageIndex = (idx >= 0) ? idx : 0;


        PlayerPrefs.SetInt(selectedStageKey, stage.stageID);
        PlayerPrefs.Save();

        BoardManager bm = Object.FindFirstObjectByType<BoardManager>();
        if (bm != null)
        {
            bm.LoadStage(stage.boardWidth, stage.boardHeight, stage.targetScore, stage.maxMoves);
        }
        else
        {
            Debug.LogWarning("[StageManager] BoardManager not found in scene.");
        }

        return true;
    }

    public bool LoadStageByIndex(int index)
    {
        if (stageDB == null) return false;

        StageData stage = stageDB.GetStageByIndex(index);
        if (stage == null)
        {
            Debug.LogError($"[StageManager] StageData is null at index={index}");
            return false;
        }
        currentStageIndex = index;

        return LoadStageById(stage.stageID);
    }
    // 옛 코드 호환: stageId(1-based)로 선택
    public void SelectStage(int stageId)
    {
        LoadStageById(stageId);
    }

    public bool HasNextStage()
    {
        if (stageDB == null || stageDB.Count <= 0) return false;
        return currentStageIndex < stageDB.Count - 1;
    }

    public void GoToNextStage()
    {
        if (stageDB == null || stageDB.Count <= 0) return;

        int currentIndex = (CurrentStage == null) ? -1 : stageDB.GetIndexById(CurrentStage.stageID);
        int nextIndex = Mathf.Clamp(currentIndex + 1, 0, stageDB.Count - 1);

        if (nextIndex == currentIndex) return;
        LoadStageByIndex(nextIndex);
    }
}
