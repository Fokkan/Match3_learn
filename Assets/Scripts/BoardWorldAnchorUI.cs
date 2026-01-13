using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 보드 월드 앵커(UI)가 보드 위치를 따라가도록 맞추는 스크립트.
/// - 플레이 모드에서도 동작
/// - 에디터에서도(옵션) 미리보기 동작
/// - Player 빌드에서는 MonoBehaviour.runInEditMode 접근을 완전히 배제(컴파일 에러 방지)
/// </summary>
[ExecuteAlways]
public class BoardWorldAnchorUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform targetWorld;          // 따라갈 월드 Transform (보드 중심 등)
    [SerializeField] private RectTransform uiAnchor;         // UI에서 움직일 RectTransform (보드 월드 앵커 UI)
    [SerializeField] private Canvas targetCanvas;            // Screen Space Overlay/Camera Canvas

    [Header("Edit Mode Preview")]
    [Tooltip("에디터(플레이 아님)에서도 앵커를 갱신할지 여부")]
    [SerializeField] private bool previewInEditMode = true;

    [Header("Optional")]
    [Tooltip("앵커가 화면 밖으로 나가면 숨길지 여부")]
    [SerializeField] private bool hideWhenOffscreen = false;

    private Camera cachedCam;

    private void Reset()
    {
        // 기본 추정
        targetCanvas = GetComponentInParent<Canvas>();
        uiAnchor = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        CacheCamera();

#if UNITY_EDITOR
        // IMPORTANT:
        // MonoBehaviour.runInEditMode는 Player 빌드에서 접근하면 컴파일 에러가 날 수 있어
        // 에디터에서만 접근하도록 완전히 격리한다.
        // (그리고 굳이 runInEditMode가 없어도 ExecuteAlways + Update로 동작 가능)
        // 따라서 여기서는 필요 시에만 "에디터에서" 보조적으로 켠다.
        try
        {
            // 일부 Unity 버전에서만 의미가 있으므로 안전하게 처리
            this.runInEditMode = previewInEditMode;
        }
        catch { }
#endif

        UpdateAnchor();
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        // 안전하게 원복(선택)
        try { this.runInEditMode = false; } catch { }
#endif
    }

    private void Update()
    {
        // 플레이 중이면 항상 갱신
        if (Application.isPlaying)
        {
            UpdateAnchor();
            return;
        }

        // 에디터(플레이 아님)에서는 옵션이 켜져 있을 때만
        if (previewInEditMode)
        {
            UpdateAnchor();
        }
    }

    /// <summary>
    /// 외부에서 강제로 즉시 갱신 호출할 때 사용
    /// </summary>
    public void ForceRefresh()
    {
        CacheCamera();
        UpdateAnchor();
    }

    private void CacheCamera()
    {
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
        }

        // Overlay면 Camera 필요 없음, Camera/World면 카메라 필요
        if (targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cachedCam = targetCanvas.worldCamera != null ? targetCanvas.worldCamera : Camera.main;
        }
        else
        {
            cachedCam = null; // Overlay에서는 null로 넣어도 RectTransformUtility가 처리함(카메라 미사용 경로)
        }
    }

    private void UpdateAnchor()
    {
        if (targetWorld == null || uiAnchor == null || targetCanvas == null)
            return;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cachedCam, targetWorld.position);

        RectTransform canvasRect = targetCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        Vector2 localPos;
        bool inCanvas = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            cachedCam,
            out localPos
        );

        // 위치 적용
        uiAnchor.anchoredPosition = localPos;

        // 화면 밖이면 숨김 옵션
        if (hideWhenOffscreen)
        {
            uiAnchor.gameObject.SetActive(inCanvas);
        }
        else
        {
            if (!uiAnchor.gameObject.activeSelf) uiAnchor.gameObject.SetActive(true);
        }
    }
}
