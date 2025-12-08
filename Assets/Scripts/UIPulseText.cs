using UnityEngine;
using TMPro;

public class UIPulseText : MonoBehaviour
{
    [Header("Target")]
    public TMP_Text targetText;        // 애니메이션 줄 텍스트 (비우면 자기 자신)

    [Header("Pulse Settings")]
    [SerializeField] private float scaleAmplitude = 0.15f; // 커졌다/작아지는 정도
    [SerializeField] private float scaleSpeed = 3f;        // 속도

    [Header("Blink Settings")]
    [SerializeField] private float alphaAmplitude = 0.4f;  // 투명도 변화량 (0~1)
    [SerializeField] private float alphaSpeed = 2f;        // 속도

    private Vector3 baseScale;
    private Color baseColor;

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        baseScale = transform.localScale;

        if (targetText != null)
            baseColor = targetText.color;
    }

    private void OnEnable()
    {
        // 다시 켜질 때 원래 상태로 초기화
        transform.localScale = baseScale;

        if (targetText != null)
            targetText.color = baseColor;
    }

    private void Update()
    {
        if (targetText == null) return;

        float t = Time.time;

        // ---- 1) 스케일 펄스 ----
        float scaleOffset = Mathf.Sin(t * scaleSpeed) * scaleAmplitude;
        float scale = 1f + scaleOffset;
        transform.localScale = baseScale * scale;

        // ---- 2) 알파 깜빡임 ----
        Color c = baseColor;
        float alphaOffset = (Mathf.Sin(t * alphaSpeed) + 1f) * 0.5f; // 0~1
        // 1 - alphaAmplitude ~ 1 범위로 맵핑
        float minAlpha = 1f - alphaAmplitude;
        c.a = Mathf.Lerp(minAlpha, 1f, alphaOffset);

        targetText.color = c;
    }
}
