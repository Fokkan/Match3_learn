using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Database")]
    public StageDatabase stageDB;      // StageDB.asset 연결

    [Header("Current Stage")]
    public int currentStageIndex = 0;  // 0 기반 인덱스 (Stage 1 = 0, Stage 2 = 1 ...)

    [Header("Board Reference")]
    public BoardManager board;

    // --------------------------------------------------
    // 현재 스테이지 데이터 가져오기
    // --------------------------------------------------
    public StageData CurrentStage
    {
        get
        {
            if (stageDB == null || stageDB.stages == null || stageDB.stages.Length == 0)
                return null;

            currentStageIndex = Mathf.Clamp(currentStageIndex, 0, stageDB.stages.Length - 1);
            return stageDB.stages[currentStageIndex];
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // 이제는 자동으로 스테이지를 깔지 않는다.
        // 플레이어가 Stage 버튼을 눌렀을 때만 SelectStage()를 통해 보드를 생성.
    }

    // --------------------------------------------------
    // 보드에 현재 스테이지 정보 적용
    // --------------------------------------------------
    public void ApplyCurrentStageToBoard()
    {
        if (board == null) return;

        StageData s = CurrentStage;
        if (s == null) return;

        board.LoadStage(
            s.boardWidth,
            s.boardHeight,
            s.targetScore,
            s.maxMoves
        );
    }

    // --------------------------------------------------
    // 스테이지 선택 (버튼에서 호출)
    // --------------------------------------------------
    public void SelectStage(int stageIndex)
    {
        if (stageDB == null || stageDB.stages == null || stageDB.stages.Length == 0)
            return;

        stageIndex = Mathf.Clamp(stageIndex, 0, stageDB.stages.Length - 1);
        currentStageIndex = stageIndex;

        ApplyCurrentStageToBoard();
    }

    // --------------------------------------------------
    // 다음 스테이지 여부 / 이동
    // --------------------------------------------------
    public bool HasNextStage()
    {
        if (stageDB == null || stageDB.stages == null) return false;
        return currentStageIndex < stageDB.stages.Length - 1;
    }

    public void GoToNextStage()
    {
        if (!HasNextStage()) return;

        currentStageIndex++;
        ApplyCurrentStageToBoard();
    }
}
