using UnityEngine;

[ExecuteAlways]
public class WorldBoardFitterV3 : MonoBehaviour
{
    [SerializeField] private RectTransform uiArea;
    [SerializeField] private Transform boardRoot;
    [SerializeField] private SpriteRenderer boardPlateRenderer;
    [SerializeField] private Camera worldCamera;

    [SerializeField] private float paddingWorld = 0.10f;
    [SerializeField] private float minScale = 0.35f;
    [SerializeField] private float maxScale = 1.00f;

    // (추가) 보드를 허용영역에 "꽉" 채우지 않고 일부러 덜 채우는 비율
    // 0.85~0.90 추천
    [SerializeField, Range(0.6f, 1.0f)]
    private float fillRatio = 0.88f;

    [Header("Stabilization")]
    [SerializeField] private float scaleEpsilon = 0.001f;
    [SerializeField] private bool runInEditMode = false;

    private Canvas cachedCanvas;

    // scale=1 기준 plate 월드 크기(불변)
    private Vector2 basePlateSizeWorld;

    // (추가) Sliced/Tiled size 변경 감지용
    private Vector2 lastPlateSizeLocal;
    private const float plateSizeEpsilon = 0.00001f;

    private void Reset()
    {
        uiArea = GetComponent<RectTransform>();
        worldCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (uiArea != null)
            cachedCanvas = uiArea.GetComponentInParent<Canvas>();

        RebuildBaseSize();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying && !runInEditMode) return;

        if (uiArea == null || boardRoot == null || boardPlateRenderer == null || worldCamera == null)
            return;

        if (cachedCanvas == null)
            cachedCanvas = uiArea.GetComponentInParent<Canvas>();

        Canvas.ForceUpdateCanvases();

        // (추가) Stage 로드/UpdateBoardPlate로 plateRenderer.size가 바뀌면 base 사이즈 재계산
        if (HasPlateSizeChanged())
            RebuildBaseSize();

        if (basePlateSizeWorld.x <= 0.0001f || basePlateSizeWorld.y <= 0.0001f)
            RebuildBaseSize();

        Fit();
    }

    private bool HasPlateSizeChanged()
    {
        // Sliced/Tiled일 때 size가 실제 "로컬 단위" 크기라서 이걸 추적하는 게 가장 안정적
        if (boardPlateRenderer == null) return false;

        // Unity는 Simple이어도 size 접근은 되지만 의미가 약함.
        // 그래도 현재 프로젝트는 UpdateBoardPlate에서 Sliced를 쓰고 있으니 이 조건으로 충분.
        Vector2 cur = boardPlateRenderer.size;
        return (cur - lastPlateSizeLocal).sqrMagnitude > plateSizeEpsilon;
    }

    private void RebuildBaseSize()
    {
        if (boardRoot == null || boardPlateRenderer == null) return;

        // Sliced/Tiled이면 bounds로 나눠서 역산하지 말고,
        // SpriteRenderer.size(로컬 단위)를 scale=1 기준 크기로 쓰는 게 제일 안정적
        Vector2 localSize = boardPlateRenderer.size;

        if (localSize.x > 0.0001f && localSize.y > 0.0001f)
        {
            basePlateSizeWorld = localSize;
            lastPlateSizeLocal = localSize;
            return;
        }

        // fallback: sprite bounds
        if (boardPlateRenderer.sprite != null)
        {
            basePlateSizeWorld = boardPlateRenderer.sprite.bounds.size;
            lastPlateSizeLocal = basePlateSizeWorld;
        }
    }

    private void Fit()
    {
        Vector3[] corners = new Vector3[4];
        uiArea.GetWorldCorners(corners);

        Vector2 blScreen, trScreen;

        if (cachedCanvas != null && cachedCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            blScreen = new Vector2(corners[0].x, corners[0].y);
            trScreen = new Vector2(corners[2].x, corners[2].y);
        }
        else
        {
            Camera uiCam = (cachedCanvas != null) ? cachedCanvas.worldCamera : null;
            blScreen = RectTransformUtility.WorldToScreenPoint(uiCam, corners[0]);
            trScreen = RectTransformUtility.WorldToScreenPoint(uiCam, corners[2]);
        }

        float zDist = Mathf.Abs(worldCamera.transform.position.z - boardRoot.position.z);

        Vector3 blWorld = worldCamera.ScreenToWorldPoint(new Vector3(blScreen.x, blScreen.y, zDist));
        Vector3 trWorld = worldCamera.ScreenToWorldPoint(new Vector3(trScreen.x, trScreen.y, zDist));

        float allowedW = Mathf.Max(0.0001f, (trWorld.x - blWorld.x) - paddingWorld * 2f);
        float allowedH = Mathf.Max(0.0001f, (trWorld.y - blWorld.y) - paddingWorld * 2f);
        Vector3 allowedCenter = (blWorld + trWorld) * 0.5f;

        // targetScale 계산
        float target = Mathf.Min(allowedW / basePlateSizeWorld.x, allowedH / basePlateSizeWorld.y);

        // (네가 요청한 "정확한 위치") 여기서 1줄: 의도적으로 덜 키우기
        target *= fillRatio;

        target = Mathf.Clamp(target, minScale, maxScale);

        float current = boardRoot.localScale.x;
        if (Mathf.Abs(current - target) > scaleEpsilon)
        {
            boardRoot.localScale = Vector3.one * target;
        }

        // 중심 정렬
        Bounds b2 = boardPlateRenderer.bounds;
        Vector3 pos = boardRoot.position;
        pos.x += (allowedCenter.x - b2.center.x);
        pos.y += (allowedCenter.y - b2.center.y);
        boardRoot.position = pos;
    }
}
