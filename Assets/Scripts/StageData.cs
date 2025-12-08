using UnityEngine;

[System.Serializable]
public class StageData
{
    public int stageID;            // 스테이지 번호
    public int targetScore;        // 목표 점수
    public int maxMoves;           // 제한 움직임 수
    public int boardWidth = 8;     // 추후 스테이지마다 다른 보드로 확장 가능
    public int boardHeight = 8;
}
