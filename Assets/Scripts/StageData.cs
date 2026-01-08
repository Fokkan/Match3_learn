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

    [Header("Stage Base")]
    public int stageID;         // 표기용 스테이지 번호
    public int targetScore;     // 목표 점수(현재 정책에서는 보통 클리어 조건에 미사용)
    public int maxMoves;        // 제한 횟수
    public int boardWidth = 8;  // 보드 가로
    public int boardHeight = 8; // 보드 세로

    [Header("Obstacles - Ice")]
    public bool useObstacles = false; // ICE 사용 여부
    public int obstacleCount = 0;     // 랜덤 ICE 개수(iceCage 없을 때)
    public int obstacleLevel = 0;     // 클러스터 룰 레벨(0~)

    [Header("Goal - Collect (2~4 colors recommended)")]
    public bool useCollectGoal = false;

    // Collect만 달성하면 클리어면 보통 false 유지(점수 조건 막지 않음)
    public bool requirePassScore = false;

    [Serializable]
    public struct CollectTarget
    {
        // gemSprites 인덱스(= Gem.type) 기준
        public int gemType;
        public int target;
    }

    // 스테이지마다 2~4개 권장
    public CollectTarget[] collectTargets;
}
