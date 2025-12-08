using UnityEngine;
using DG.Tweening;

// 스페셜 젬 타입 정의 (BoardManager에서도 이 enum을 사용)
public enum SpecialGemType
{
    None,
    RowBomb,
    ColBomb,
    ColorBomb
}

public class Gem : MonoBehaviour
{
    [Header("Grid Position")]
    public int x;
    public int y;

    [Header("Gem Info")]
    public int type;                 // 색/종류 인덱스
    public BoardManager board;       // 소속 보드
    public Color baseColor = Color.white;

    [Header("Special")]
    public SpecialGemType specialType = SpecialGemType.None;
    public bool IsSpecial => specialType != SpecialGemType.None;
    public bool IsColorBomb => specialType == SpecialGemType.ColorBomb;
    public bool IsRowBomb => specialType == SpecialGemType.RowBomb;
    public bool IsColBomb => specialType == SpecialGemType.ColBomb;

    [Header("Sprite Settings")]
    public Sprite normalSprite;
    public Sprite rowBombSprite;
    public Sprite colBombSprite;
    public Sprite colorBombSprite;

    [Header("Tween Settings")]
    public float selectScale = 1.15f;
    public float fallDuration = 0.25f;

    private SpriteRenderer sr;
    private Tween selectTween;
    private Tween hintTween;
    private Vector3 originalScale;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // 현재 스케일을 원본으로 저장
        originalScale = transform.localScale;

        if (sr != null)
        {
            baseColor = sr.color;
            baseColor.a = 1f;
            sr.color = baseColor;

            if (normalSprite == null)
                normalSprite = sr.sprite;
        }
    }


    // 보드에서 이 젬을 초기화할 때 호출
    public void Init(BoardManager board, int x, int y, int type)
    {
        this.board = board;
        this.x = x;
        this.y = y;
        this.type = type;

        specialType = SpecialGemType.None;

        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            baseColor = sr.color;
            baseColor.a = 1f;
            sr.color = baseColor;
        }
    }

    // 현재 x,y 를 기준으로 격자 위치에 딱 붙이기
    public void SnapToGrid()
    {
        if (board == null) return;

        float offsetX = (board.width - 1) * 0.5f;
        float offsetY = (board.height - 1) * 0.5f;

        transform.localPosition = new Vector3(
            x - offsetX,
            y - offsetY,
            0f
        );
    }

    // 격자 좌표 변경 + 위치 이동
    public void SetGridPosition(int newX, int newY, bool animate = true, float duration = 0.15f)
    {
        x = newX;
        y = newY;

        if (board == null)
        {
            transform.localPosition = new Vector3(x, y, 0f);
            return;
        }

        float offsetX = (board.width - 1) * 0.5f;
        float offsetY = (board.height - 1) * 0.5f;

        Vector3 targetPos = new Vector3(
            x - offsetX,
            y - offsetY,
            0f
        );

        transform.DOKill();

        if (animate)
        {
            transform.DOLocalMove(targetPos, duration)
                     .SetEase(Ease.OutQuad);
        }
        else
        {
            transform.localPosition = targetPos;
        }
    }

    // BoardManager에서 자주 쓰는 간단 버전 오버로드
    public void SetGridPosition(int newX, int newY)
    {
        SetGridPosition(newX, newY, true, fallDuration);
    }

    // 시각 상태를 항상 초기 상태로 복구
    public void ResetVisual()
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        sr.DOKill();
        if (selectTween != null) selectTween.Kill();
        if (hintTween != null) hintTween.Kill();

        transform.DOKill();

        sr.color = baseColor;
        transform.localScale = originalScale;
    }

    // 선택 효과
    public void SetSelected(bool selected)
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        if (selectTween != null)
        {
            selectTween.Kill();
            selectTween = null;
        }

        if (!selected)
        {
            ResetVisual();
            return;
        }

        // 선택 시: 먼저 시각 상태 리셋 후, 색만 살짝 밝게
        ResetVisual();

        Color c = baseColor * 1.2f;
        c.a = 1f;
        sr.color = c;
    }



    // 힌트 효과 시작
    public void PlayHintEffect()
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        ResetVisual();

        // 완전히 0까지는 내리지 않고 0.3까지 깜박이기
        hintTween = sr.DOFade(0.3f, 0.4f)
                     .SetLoops(-1, LoopType.Yoyo);
    }

    // 힌트 효과 종료
    public void StopHintEffect()
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        if (hintTween != null)
        {
            hintTween.Kill();
            hintTween = null;
        }

        ResetVisual();
    }

    // 스페셜 젬 타입 설정 (스프라이트만 교체)
    public void SetSpecial(SpecialGemType type)
    {
        specialType = type;

        if (sr == null)
            sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        // 스페셜 젬은 색이 들어가면 안 됨 → 무조건 순백색
        sr.color = Color.white;

        Sprite target = normalSprite;

        switch (type)
        {
            case SpecialGemType.None:
                target = normalSprite;
                break;

            case SpecialGemType.RowBomb:
                target = rowBombSprite != null ? rowBombSprite : normalSprite;
                break;

            case SpecialGemType.ColBomb:
                target = colBombSprite != null ? colBombSprite : normalSprite;
                break;

            case SpecialGemType.ColorBomb:
                target = colorBombSprite != null ? colorBombSprite : normalSprite;
                break;
        }

        sr.sprite = target;
    }



    // 마우스로 클릭했을 때 BoardManager에 알리기
    private void OnMouseDown()
    {
        if (board != null)
        {
            board.OnGemClicked(this);
        }
    }
}
