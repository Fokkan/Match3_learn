using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class CollectTarget
{
    public int gemType;
    public int target;
}

[Serializable]
public class StageData
{
    // YAML: stageID
    [FormerlySerializedAs("stageId")]
    public int stageID = 1;

    // (선택) 설명 텍스트
    [TextArea] public string stageDescription;

    public int targetScore = 500;
    public int maxMoves = 20;

    public int boardWidth = 5;
    public int boardHeight = 5;

    public bool useObstacles = false;
    public int obstacleCount = 0;
    public int obstacleLevel = 0;

    public bool useCollectGoal = false;

    // YAML은 0/1로 저장돼도 bool로 문제 없이 로드됩니다.
    public bool requirePassScore = false;

    public List<CollectTarget> collectTargets = new List<CollectTarget>();

    // YAML: iceCage
    public int[] iceCage;
}
