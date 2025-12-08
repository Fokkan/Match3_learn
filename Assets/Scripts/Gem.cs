using UnityEngine;
using DG.Tweening;

// 스페셜 젬 타입 정의 (BoardManager에서도 이 enum을 사용)
public enum SpecialGemType
{
    None,
    RowBomb,
    ColBomb,
    ColorBomb,
    WrappedBomb        // 이름을 일관되게 PascalCase로 정리
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
    public bool IsWrappedBomb => specialType == SpecialGemType.WrappedBomb;

    [Header("Sprite Settings")]
    public SpriteRenderer sr;        // 인스펙터에서 SpriteRenderer 연결
    public Sprite normalSprite;
    public Sprite rowBombSprite;
    public Sprite colBombSprite;
    public Sprite wrappedBombSprite;
    public Sprite colorBombSprite;

    [Header("Tween Settings")]
    public float selectScale = 1.15f;
    public float fallDuration = 0.25f;

    private Tween selectTween;
    private Tween hintTween;
    private Vector3 originalScale;

    private void Awake()
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

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

        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        // 기본 젬 스프라이트
        if (board != null && board.gemSprites != null &&
            type >= 0 && type < board.gemSprites.Length)
        {
            sr.sprite = board.gemSprites[type];
        }

        normalSprite = sr.sprite;
        specialType = SpecialGemType.None;
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

    // 시각 상태 리셋
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

    // 스페셜 젬 타입 설정 (스프라이트 교체)
    public void SetSpecial(SpecialGemType newType)
    {
        specialType = newType;

        if (sr == null)
            sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            return;

        Sprite target = normalSprite;

        switch (specialType)
        {
            case SpecialGemType.None:
                target = normalSprite;
                break;

            case SpecialGemType.RowBomb:
                if (rowBombSprite != null) target = rowBombSprite;
                break;

            case SpecialGemType.ColBomb:
                if (colBombSprite != null) target = colBombSprite;
                break;

            case SpecialGemType.WrappedBomb:
                if (wrappedBombSprite != null) target = wrappedBombSprite;
                break;

            case SpecialGemType.ColorBomb:
                if (colorBombSprite != null) target = colorBombSprite;
                break;
        }

        if (target != null)
            sr.sprite = target;

        Debug.Log($"SetSpecial -> {specialType}, sprite = {sr.sprite?.name}");
    }

    // 타입/스페셜 강제 세팅 (셔플 등에서 사용)
    public void ForceSetType(int newType, SpecialGemType special = SpecialGemType.None)
    {
        type = newType;

        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        // 기본 스프라이트 갱신
        if (board != null && board.gemSprites != null &&
            newType >= 0 && newType < board.gemSprites.Length)
        {
            sr.sprite = board.gemSprites[newType];
            normalSprite = sr.sprite;
        }

        // 특수 타입 적용 + 스프라이트 교체
        SetSpecial(special);
    }

    private void OnMouseDown()
    {
        if (board != null)
        {
            board.OnGemClicked(this);
        }
    }
}
