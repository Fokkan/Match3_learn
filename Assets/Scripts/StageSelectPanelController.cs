using UnityEngine;
using DG.Tweening;

public class StageSelectPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float hideDuration = 0.7f;       // 효과 느껴지게 조금 길게
    [SerializeField] private Ease hideEase = Ease.OutQuad;

    [Header("Game Logic")]
    [SerializeField] private StageManager stageManager;

    private bool isPlayingAnim = false;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnClickStageButton(int stageIndex)
    {
        if (isPlayingAnim) return;
        isPlayingAnim = true;

        Debug.Log($"[StageSelectPanel] Stage button clicked: {stageIndex}");

        if (!canvasGroup)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // 애니 시작 전 기본값
        transform.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();

        if (canvasGroup != null)
        {
            // 알파 페이드
            seq.Join(
                canvasGroup.DOFade(0f, hideDuration)
                           .SetEase(hideEase)
            );
        }

        // 살짝 줄어들면서 사라지는 느낌
        seq.Join(
            transform.DOScale(0.8f, hideDuration)
                     .SetEase(hideEase)
        );

        seq.OnComplete(() =>
        {
            Debug.Log("[StageSelectPanel] Hide animation complete");

            gameObject.SetActive(false);

            if (stageManager != null)
            {
                stageManager.SelectStage(stageIndex);
            }
        });
    }
}
