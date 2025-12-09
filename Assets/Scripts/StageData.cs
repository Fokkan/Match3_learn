[System.Serializable]
public class StageData
{
    // 스테이지 기본 정보
    public int stageID;           // 스테이지 번호
    public int targetScore;       // 목표 점수
    public int maxMoves;          // 제한 횟수
    public int boardWidth = 8;   // 보드 가로 크기
    public int boardHeight = 8;   // 보드 세로 크기

    // --- 여기부터 방해요소용 데이터 추가 ---
    public bool useObstacles = false; // 이 스테이지에서 방해요소를 쓸지 여부
    public int obstacleCount = 0;     // 장애물 대략 몇 개 깔지
    public int obstacleLevel = 0;     // 난이도/단계 (0=없음, 1,2,3...)
}
