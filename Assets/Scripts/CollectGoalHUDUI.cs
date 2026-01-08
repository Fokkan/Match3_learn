using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CollectGoalHUDUI : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public GameObject root;
        public Image icon;
        public TMP_Text remainText; // 남은 개수(= target - collected)
    }

    [Header("Slots (Max 4)")]
    public Slot[] collectSlots;

    [Header("Ice Slot (Optional)")]
    public GameObject iceRoot;
    public TMP_Text iceRemainText;

    [Header("Visibility (Recommended)")]
    [Tooltip("패널 전체를 On/Off 하지 않고 투명도로 숨기기 위한 CanvasGroup")]
    public CanvasGroup canvasGroup;

    private BoardManager board;
    private Coroutine bindRoutine;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (bindRoutine != null) StopCoroutine(bindRoutine);
        bindRoutine = StartCoroutine(BindWhenReady());
    }

    private void OnDisable()
    {
        if (bindRoutine != null) StopCoroutine(bindRoutine);
        bindRoutine = null;

        if (board != null)
            board.OnGoalProgressChanged -= Refresh;

        board = null;
    }

    private IEnumerator BindWhenReady()
    {
        // 시작은 숨김(슬롯만)
        SetVisible(false);

        while (board == null)
        {
            if (StageManager.Instance != null && StageManager.Instance.board != null)
                board = StageManager.Instance.board;

            yield return null;
        }

        board.OnGoalProgressChanged += Refresh;

        // 첫 갱신
        Refresh();
    }

    private void SetVisible(bool v)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = v ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false; // 보드 입력 방해 금지
        }
        else
        {
            // CanvasGroup이 없으면 root active는 유지하고 슬롯만 관리
        }
    }

    private void SetAllCollectSlotsActive(bool active)
    {
        if (collectSlots == null) return;

        for (int i = 0; i < collectSlots.Length; i++)
        {
            if (collectSlots[i]?.root != null)
                collectSlots[i].root.SetActive(active);
        }
    }

    public void Refresh()
    {
        if (board == null)
        {
            SetVisible(false);
            return;
        }

        // Collect 목표가 있는지 확인
        bool hasCollect = board.TryGetCollectGoalData(out int[] types, out int[] targets, out int[] collected);

        // Ice 목표가 있는지 확인
        bool hasIce = board.GetTotalIce() > 0;

        // 아무 목표도 없으면 바 자체를 숨김
        if (!hasCollect && !hasIce)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        // Collect 슬롯 갱신
        if (hasCollect && collectSlots != null && collectSlots.Length > 0)
        {
            int count = Mathf.Min(types.Length, collectSlots.Length);

            for (int i = 0; i < collectSlots.Length; i++)
            {
                bool active = (i < count);
                if (collectSlots[i]?.root != null) collectSlots[i].root.SetActive(active);
                if (!active) continue;

                int t = types[i];
                int goal = targets[i];
                int got = collected[i];
                int remain = Mathf.Max(0, goal - got);

                if (collectSlots[i].icon != null && board.gemSprites != null && t >= 0 && t < board.gemSprites.Length)
                    collectSlots[i].icon.sprite = board.gemSprites[t];

                if (collectSlots[i].remainText != null)
                    collectSlots[i].remainText.text = remain.ToString();
            }
        }
        else
        {
            // Collect 목표가 없으면 Collect 슬롯은 전부 숨김
            SetAllCollectSlotsActive(false);
        }

        // Ice 슬롯 갱신
        if (iceRoot != null)
        {
            iceRoot.SetActive(hasIce);

            if (hasIce && iceRemainText != null)
                iceRemainText.text = board.GetIceRemaining().ToString();
        }
    }
}
