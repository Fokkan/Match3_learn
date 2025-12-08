using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage List")]
    public List<StageData> stages = new List<StageData>();

    [Header("Current Stage")]
    public int currentStageIndex = 0;

    [Header("Board Reference")]
    public BoardManager board;

    // 현재 스테이지 편하게 가져오기
    public StageData CurrentStage
    {
        get
        {
            if (stages == null || stages.Count == 0)
                return null;

            currentStageIndex = Mathf.Clamp(currentStageIndex, 0, stages.Count - 1);
            return stages[currentStageIndex];
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
        // 한 씬만 쓸 거면 DontDestroyOnLoad 는 필요 없음
        //DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 게임 시작 시 0번 스테이지 적용
        //ApplyCurrentStageToBoard();
    }

    // ===== 보드에 현재 스테이지 정보 적용 =====
    public void ApplyCurrentStageToBoard()
    {
        if (board == null) return;

        StageData s = CurrentStage;
        if (s == null) return;
        board.ResetState();
        board.LoadStage(
            s.boardWidth,
            s.boardHeight,
            s.targetScore,
            s.maxMoves
        );
    }

    // ===== 스테이지 선택(버튼에서 호출) =====
    public void SelectStage(int stageIndex)
    {
        if (stages == null || stages.Count == 0)
            return;

        stageIndex = Mathf.Clamp(stageIndex, 0, stages.Count - 1);
        currentStageIndex = stageIndex;

        ApplyCurrentStageToBoard();
    }

    // ===== 다음 스테이지 관련 =====
    public bool HasNextStage()
    {
        if (stages == null) return false;
        return currentStageIndex < stages.Count - 1;
    }

    public void GoToNextStage()
    {
        if (!HasNextStage()) return;

        currentStageIndex++;
        ApplyCurrentStageToBoard();
    }
}
