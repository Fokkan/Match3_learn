using UnityEngine;
using DG.Tweening;

/// <summary>
/// 개별 젬 오브젝트.
/// - 격자 위치(x,y)
/// - 색상/타입(type)
/// - 스페셜 타입(줄폭탄, 컬러봄 등)
/// - 선택/힌트/드롭 연출
/// 실제 매치/판정 로직은 BoardManager에서 처리하고,
/// Gem은 시각적인 표현과 자신의 상태만 관리한다.
/// </summary>
public enum SpecialGemType
{
    None,
    RowBomb,
    ColBomb,
    ColorBomb,
    WrappedBomb
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

    // 스페셜 타입 헬퍼 프로퍼티
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

    [Header("Stripe Sprites By Type")]
    public Sprite[] rowBombSpritesByType; // type 인덱스 기준 (0=Red,1=Blue,...)
    public Sprite[] colBombSpritesByType;

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

    /// <summary>
    /// 보드에서 젬을 스폰할 때 호출되는 초기화 함수.
    /// - 보드 참조
    /// - 격자 좌표
    /// - 타입(색상)
    /// 를 세팅하고 기본 스프라이트를 지정한다.
    /// </summary>
    public void Init(BoardManager board, int x, int y, int type)
    {
        this.board = board;
        this.x = x;
        this.y = y;
        this.type = type;

        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        if (board != null && board.gemSprites != null &&
            type >= 0 && type < board.gemSprites.Length)
        {
            sr.sprite = board.gemSprites[type];
        }

        normalSprite = sr.sprite;
        specialType = SpecialGemType.None;
    }

    /// <summary>
    /// 현재 x,y 기준으로 즉시 격자 위치로 이동.
    /// 초기 보드 생성이나 셔플 직후 정렬용으로 사용.
    /// </summary>
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

    /// <summary>
    /// 격자 좌표를 변경하고, 지정 시간 동안 애니메이션으로 이동.
    /// </summary>
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

    /// <summary>
    /// 기본 이동 시간(fallDuration)을 사용하는 간단 오버로드.
    /// </summary>
    public void SetGridPosition(int newX, int newY)
    {
        SetGridPosition(newX, newY, true, fallDuration);
    }

    /// <summary>
    /// 선택/힌트 등으로 변경된 시각 상태를 초기값으로 되돌린다.
    /// </summary>
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

    /// <summary>
    /// 젬 선택 상태 토글.
    /// 선택 시 색을 살짝 밝게, 선택 해제 시 원상 복구.
    /// </summary>
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

    /// <summary>
    /// 일정 시간 후 반짝이는 힌트 효과 시작.
    /// </summary>
    public void PlayHintEffect()
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        ResetVisual();

        hintTween = sr.DOFade(0.3f, 0.4f)
                     .SetLoops(-1, LoopType.Yoyo);
    }

    /// <summary>
    /// 힌트 효과 종료 및 원상 복귀.
    /// </summary>
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

    /// <summary>
    /// 스페셜 젬 타입 설정 + 대응 스프라이트로 교체.
    /// Row/Col는 타입별 스프라이트가 우선 적용되고,
    /// 없으면 공통 스프라이트를 사용한다.
    /// </summary>
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
                if (rowBombSpritesByType != null &&
                    type >= 0 && type < rowBombSpritesByType.Length &&
                    rowBombSpritesByType[type] != null)
                {
                    target = rowBombSpritesByType[type];
                }
                else if (rowBombSprite != null)
                {
                    target = rowBombSprite;
                }
                break;

            case SpecialGemType.ColBomb:
                if (colBombSpritesByType != null &&
                    type >= 0 && type < colBombSpritesByType.Length &&
                    colBombSpritesByType[type] != null)
                {
                    target = colBombSpritesByType[type];
                }
                else if (colBombSprite != null)
                {
                    target = colBombSprite;
                }
                break;

            case SpecialGemType.WrappedBomb:
                if (wrappedBombSprite != null)
                    target = wrappedBombSprite;
                break;

            case SpecialGemType.ColorBomb:
                if (colorBombSprite != null)
                    target = colorBombSprite;
                break;
        }

        if (target != null)
            sr.sprite = target;
    }

    /// <summary>
    /// 셔플 등에서 강제로 타입/스페셜을 세팅할 때 사용하는 함수.
    /// 기본 스프라이트 갱신 후 SetSpecial을 통해 스페셜 반영.
    /// </summary>
    public void ForceSetType(int newType, SpecialGemType special = SpecialGemType.None)
    {
        type = newType;

        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        if (board != null && board.gemSprites != null &&
            newType >= 0 && newType < board.gemSprites.Length)
        {
            sr.sprite = board.gemSprites[newType];
            normalSprite = sr.sprite;
        }

        SetSpecial(special);
    }

    /// <summary>
    /// Gem 클릭 시 BoardManager에 위임.
    /// (모바일 터치 시에는 Raycast 기반으로 처리 가능)
    /// </summary>
    private void OnMouseDown()
    {
        if (board != null)
        {
            board.OnGemClicked(this);
        }
    }
}
