using UnityEngine;
using DG.Tweening;

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
    public int type;
    public BoardManager board;
    public Color baseColor = Color.white;

    [Header("Special")]
    public SpecialGemType specialType = SpecialGemType.None;

    public bool IsSpecial => specialType != SpecialGemType.None;
    public bool IsColorBomb => specialType == SpecialGemType.ColorBomb;
    public bool IsRowBomb => specialType == SpecialGemType.RowBomb;
    public bool IsColBomb => specialType == SpecialGemType.ColBomb;
    public bool IsWrappedBomb => specialType == SpecialGemType.WrappedBomb;

    [Header("Sprite Settings")]
    public SpriteRenderer sr;

    public Sprite normalSprite;

    public Sprite rowBombSprite;
    public Sprite colBombSprite;
    public Sprite wrappedBombSprite;
    public Sprite colorBombSprite;

    [Header("Special Sprites By Type")]
    public Sprite[] rowBombSpritesByType;
    public Sprite[] colBombSpritesByType;
    public Sprite[] wrappedBombSpritesByType;

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

    public void Init(BoardManager board, int x, int y, int type)
    {
        this.board = board;
        this.x = x;
        this.y = y;
        this.type = type;

        EnsureSpriteRenderer();

        normalSprite = ResolveNormalSprite(type);
        specialType = SpecialGemType.None;

        ApplyVisualByState();
    }

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
    public void SetGridPositionFlow(
    int newX, int newY,
    float duration,
    float xSettleRatio = 0.55f,
    Ease easeY = Ease.InQuad,
    Ease easeX = Ease.OutQuad,
    bool landingSquash = true,
    float squashScaleY = 0.92f,
    float squashTime = 0.06f)
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

        Vector3 targetPos = new Vector3(x - offsetX, y - offsetY, 0f);

        transform.DOKill();

        // Y는 가속(흘러내림), X는 빨리 정착
        float durY = Mathf.Max(0.01f, duration);
        float durX = Mathf.Max(0.01f, duration * xSettleRatio);

        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOLocalMoveY(targetPos.y, durY).SetEase(easeY));
        seq.Join(transform.DOLocalMoveX(targetPos.x, durX).SetEase(easeX));

        if (landingSquash)
        {
            // 원래 스케일이 없다면 현재 스케일을 기준으로
            Vector3 baseScale = (originalScale == Vector3.zero) ? transform.localScale : originalScale;

            seq.AppendCallback(() =>
            {
                transform.localPosition = new Vector3(targetPos.x, transform.localPosition.y, 0f);
            });

            // 착지 스쿼시(선택)
            seq.Append(transform.DOScale(new Vector3(baseScale.x * 1.06f, baseScale.y * squashScaleY, baseScale.z), squashTime)
                .SetEase(Ease.OutQuad));
            seq.Append(transform.DOScale(baseScale, squashTime).SetEase(Ease.OutQuad));
        }

        seq.OnComplete(() =>
        {
            transform.localPosition = targetPos;
        });
    }

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

    public void SetGridPosition(int newX, int newY)
    {
        SetGridPosition(newX, newY, true, fallDuration);
    }

    public void ResetVisual(bool killMoveTween = false)
    {
        if (selectTween != null && selectTween.IsActive()) selectTween.Kill();
        if (hintTween != null && hintTween.IsActive()) hintTween.Kill();

        if (killMoveTween)
            transform.DOKill();

        transform.localScale = originalScale;

        if (sr != null)
            sr.color = Color.white;
    }

    public void SetSelected(bool selected)
    {
        EnsureSpriteRenderer();
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

    public void PlayHintEffect()
    {
        // 낙하 이동 트윈은 끊지 않는다
        ResetVisual(killMoveTween: false);

        if (sr == null) return;

        hintTween = sr.DOFade(0.35f, 0.35f)
                     .SetLoops(-1, LoopType.Yoyo)
                     .SetEase(Ease.InOutSine);
    }

    public void StopHintEffect()
    {
        if (hintTween != null && hintTween.IsActive())
            hintTween.Kill();

        // 낙하 이동 트윈은 끊지 않는다
        ResetVisual(killMoveTween: false);
    }

    public void SetSpecial(SpecialGemType newType)
    {
        specialType = newType;
        ApplyVisualByState();
    }

    public void ForceSetType(int newType, SpecialGemType special = SpecialGemType.None)
    {
        type = newType;
        normalSprite = ResolveNormalSprite(newType);

        specialType = special;
        ApplyVisualByState();
    }

    private void OnMouseDown()
    {
        if (board != null)
        {
            board.OnGemClicked(this);
        }
    }

    private void EnsureSpriteRenderer()
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();
    }

    private Sprite ResolveNormalSprite(int t)
    {
        EnsureSpriteRenderer();

        if (board != null && board.gemSprites != null &&
            t >= 0 && t < board.gemSprites.Length &&
            board.gemSprites[t] != null)
        {
            return board.gemSprites[t];
        }

        if (sr != null && sr.sprite != null)
            return sr.sprite;

        return normalSprite;
    }

    private Sprite ResolveSpriteByState()
    {
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
                else
                {
                    target = normalSprite;
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
                else
                {
                    target = normalSprite;
                }
                break;

            case SpecialGemType.WrappedBomb:
                if (wrappedBombSpritesByType != null &&
                    type >= 0 && type < wrappedBombSpritesByType.Length &&
                    wrappedBombSpritesByType[type] != null)
                {
                    target = wrappedBombSpritesByType[type]; //  타입별 Wrapped 우선
                }
                else
                {
                    target = (wrappedBombSprite != null) ? wrappedBombSprite : normalSprite; // fallback
                }
                break;


            case SpecialGemType.ColorBomb:
                target = (colorBombSprite != null) ? colorBombSprite : normalSprite;
                break;
        }

        return target;
    }

    private void ApplyVisualByState()
    {
        EnsureSpriteRenderer();
        if (sr == null) return;

        if (hintTween != null) { hintTween.Kill(); hintTween = null; }
        if (selectTween != null) { selectTween.Kill(); selectTween = null; }
        sr.DOKill();

        Sprite target = ResolveSpriteByState();
        if (target != null)
            sr.sprite = target;

        baseColor.a = 1f;
        sr.color = baseColor;
    }
}
