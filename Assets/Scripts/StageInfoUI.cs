using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageInfoUI : MonoBehaviour
{
    [Header("DB")]
    [SerializeField] private StageDatabase stageDB;

    [Header("Slider")]
    [SerializeField] private StageSlider stageSlider;

    [Header("UI - Title")]
    [SerializeField] private TMP_Text stageNumberText; // "STAGE 1"

    // (레거시) 예전 텍스트 한 덩어리용. 지금은 사용 안 해도 됨.
    [SerializeField] private TMP_Text stageDescriptionText;

    [Header("UI - Goal Sentence (Clean)")]
    [SerializeField] private TMP_Text goalSentenceText; // "젬 3, 젬 5, 젬 8을 각각 30개 모으세요."

    [Header("Goal UI (Dynamic)")]
    [SerializeField] private Transform collectRowRoot;          // 젬 아이콘들이 생성될 자리
    [SerializeField] private Transform obstacleRowRoot;         // 블로커 아이콘+텍스트 줄
    [SerializeField] private TMP_Text movesText;                // "20 Moves"

    [SerializeField] private GoalIconItem goalIconPrefab;       // Icon + CountText 프리팹
    [SerializeField] private Image obstacleIconImage;           // 블로커 아이콘
    [SerializeField] private TMP_Text obstacleText;             // "블로커 3개를 제거하세요"

    [Header("Sprites")]
    [SerializeField] private Sprite[] gemTypeSprites;           // index = gemType
    [SerializeField] private string[] gemTypeNames;             // index = gemType (선택, 비워도 됨)
    [SerializeField] private Sprite obstacleSprite;

    private void Awake()
    {
        if (stageSlider == null)
            stageSlider = FindFirstObjectByType<StageSlider>();
    }

    private void OnEnable()
    {
        if (stageSlider != null)
            stageSlider.OnIndexChanged += HandleIndexChanged;
    }

    private void OnDisable()
    {
        if (stageSlider != null)
            stageSlider.OnIndexChanged -= HandleIndexChanged;
    }

    private void Start()
    {
        // 최초 1회 강제 갱신
        if (stageSlider != null)
            SetStageByIndex(stageSlider.CurrentIndex);
        else
            SetStageByIndex(0);
    }

    private void HandleIndexChanged(int idx)
    {
        SetStageByIndex(idx);
    }

    public void SetStageByIndex(int index)
    {
        if (stageDB == null)
        {
            Debug.LogError("[StageInfoUI] stageDB is NULL. Inspector에서 StageDB 연결 필요.");
            return;
        }

        StageData stage = stageDB.GetStageByIndex(index);
        SetStage(stage);
    }

    public void SetStage(StageData stage)
    {
        if (stage == null) return;

        // 1) 상단 스테이지 표기
        if (stageNumberText != null)
            stageNumberText.text = $"STAGE {stage.stageID}";

        // 레거시 텍스트는 비워둠(혼선 방지)
        if (stageDescriptionText != null)
            stageDescriptionText.text = "";

        // 2) Collect 아이콘 생성
        ClearChildren(collectRowRoot);

        Dictionary<int, int> summedTargets = new Dictionary<int, int>();
        bool hasCollect = stage.useCollectGoal && stage.collectTargets != null && stage.collectTargets.Count > 0;

        if (hasCollect)
        {
            for (int i = 0; i < stage.collectTargets.Count; i++)
            {
                var t = stage.collectTargets[i];
                if (!summedTargets.ContainsKey(t.gemType)) summedTargets[t.gemType] = 0;
                summedTargets[t.gemType] += t.target;
            }

            if (collectRowRoot != null && goalIconPrefab != null)
            {
                foreach (var kv in summedTargets)
                {
                    int gemType = kv.Key;
                    int target = kv.Value;

                    var item = Instantiate(goalIconPrefab, collectRowRoot);
                    item.Set(GetGemSprite(gemType), $"x{target}");
                }
            }
        }

        // 3) “깔끔한 안내형” 문장 생성
        if (goalSentenceText != null)
        {
            goalSentenceText.text = BuildCleanGoalSentence(stage, summedTargets);
        }

        // 4) 블로커 줄
        bool hasObstacle = stage.useObstacles && stage.obstacleCount > 0;

        if (obstacleRowRoot != null)
            obstacleRowRoot.gameObject.SetActive(hasObstacle);

        if (hasObstacle)
        {
            if (obstacleIconImage != null)
                obstacleIconImage.sprite = obstacleSprite;

            if (obstacleText != null)
                obstacleText.text = $"얼음 {stage.obstacleCount}개를 제거하세요.";
        }

        // 5) Moves
        if (movesText != null)
            movesText.text = $"{stage.maxMoves} Moves";
    }

    private string BuildCleanGoalSentence(StageData stage, Dictionary<int, int> summedTargets)
    {
        // Collect 목표 우선
        if (stage.useCollectGoal && summedTargets != null && summedTargets.Count > 0)
        {
            // 동일 목표 수량이면: "젬 A, 젬 B를 각각 N개 모으세요."
            bool sameCount = true;
            int? first = null;
            foreach (var kv in summedTargets)
            {
                if (first == null) first = kv.Value;
                else if (kv.Value != first.Value) { sameCount = false; break; }
            }

            List<int> keys = new List<int>(summedTargets.Keys);
            keys.Sort();

            if (sameCount)
            {
                int n = first ?? 0;
                return $"{JoinGemNames(keys)}을 각각 {n}개 모으세요.";
            }
            else
            {
                // 서로 다르면: "젬 A 10개, 젬 B 20개를 모으세요."
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < keys.Count; i++)
                {
                    int gemType = keys[i];
                    int count = summedTargets[gemType];
                    if (i > 0) sb.Append(", ");
                    sb.Append($"{GetGemName(gemType)} {count}개");
                }
                sb.Append("를 모으세요.");
                return sb.ToString();
            }
        }

        // Collect이 아니면 점수 목표
        return $"목표 점수 {stage.targetScore}점을 달성하세요.";
    }

    private string JoinGemNames(List<int> gemTypes)
    {
        // "젬 3, 젬 5, 젬 8"
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < gemTypes.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(GetGemName(gemTypes[i]));
        }
        return sb.ToString();
    }

    private string GetGemName(int gemType)
    {
        // 이름 배열이 있으면 우선 사용, 없으면 "젬 {n}"
        if (gemTypeNames != null && gemType >= 0 && gemType < gemTypeNames.Length)
        {
            string n = gemTypeNames[gemType];
            if (!string.IsNullOrWhiteSpace(n))
                return n;
        }
        return $"젬 {gemType}";
    }

    private Sprite GetGemSprite(int gemType)
    {
        if (gemTypeSprites == null) return null;
        if (gemType < 0 || gemType >= gemTypeSprites.Length) return null;
        return gemTypeSprites[gemType];
    }

    private void ClearChildren(Transform root)
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}
