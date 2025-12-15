using System;
using UnityEngine;

/// <summary>
/// 개별 스테이지 설정 데이터.
/// ScriptableObject(StageDatabase)에 배열로 묶어서 사용한다.
/// </summary>
[Serializable]
public class StageData
{
    [Header("Obstacles - Ice Cage (0:none, 1:ice)")]
    public int[] iceCage;

    // 스테이지 기본 정보
    public int stageID;         // 스테이지 번호(표시용)
    public int targetScore;     // 목표 점수
    public int maxMoves;        // 제한 횟수
    public int boardWidth = 8;  // 보드 가로 크기
    public int boardHeight = 8; // 보드 세로 크기

    // 방해요소용 확장 데이터 (추후 사용 예정)
    public bool useObstacles = false; // 이 스테이지에서 방해요소 사용 여부
    public int obstacleCount = 0;     // 장애물 개수(대략)
    public int obstacleLevel = 0;     // 난이도/단계 (0=없음, 1,2,3...)
}
