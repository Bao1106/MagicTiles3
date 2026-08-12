using DG.Tweening;
using UnityEngine;

/// <summary>
/// A falling tile. Rendering and its own exit animation — no game rules, no timing, no Update.
///
/// NoteController moves every tile in one loop, and tells this view only which grade it died
/// with. How that death looks is entirely the view's business.
/// </summary>
public class TileView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private JudgementPalette palette;

    [Header("Hit exit")]
    [Tooltip("Peak overshoot of the hit pop, as a fraction of tile size.")]
    [SerializeField, Range(0f, 0.5f)] private float hitPunch = 0.18f;
    [SerializeField] private float hitPunchDuration = 0.06f;
    [SerializeField] private float hitFadeDuration = 0.12f;
    [Tooltip("Scale the tile expands to while dissolving.")]
    [SerializeField] private float hitDissolveScale = 1.35f;
    [Tooltip("Impact-frame colour. HDR: SpriteRenderer.color in the Inspector is LDR and cannot " +
             "exceed 1, so a value bright enough for Bloom to key off has to come from here.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color hitFlashColor = new Color(3f, 3f, 3f);

    [Header("Miss exit")]
    [SerializeField] private float missFadeDuration = 0.30f;

    [Header("Speed-up tile")]
    [Tooltip("A falling tile is read peripherally, so a special one needs a hue change rather " +
             "than a shade — a darker or lighter version of the same colour does not register " +
             "in the half second the player has.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color speedUpColor = new Color(0.25f, 1f, 0.55f);

    /// <summary>Index into NoteController's NoteData array. Set on spawn.</summary>
    public int NoteIndex { get; set; }

    /// <summary>True while the exit animation runs. The pool must not reclaim the tile yet.</summary>
    public bool IsExiting { get; private set; }

    private Vector3 baseScale = Vector3.one;
    private Color baseColor = Color.white;
    private TweenCallback onExitFinished;

    private void Awake()
    {
        baseColor = body.color;

        // Cached once per tile: a method-group-to-delegate conversion allocates on every
        // conversion, and this one would run twice a second for the whole song.
        onExitFinished = () => IsExiting = false;
    }

    /// <summary>
    /// Must be called every time the tile leaves the pool. The previous life ended at alpha 0
    /// and 1.35x scale; without this the reused tile is invisible — the classic pooling bug
    /// whose symptom is "notes randomly don't appear".
    /// </summary>
    public void ResetVisual()
    {
        KillTweens();
        IsExiting = false;
        body.color = baseColor;
        transform.localScale = baseScale;
    }

    /// <summary>Call after ResetVisual — that restores the normal colour this may override.</summary>
    public void SetKind(NoteKind kind)
    {
        body.color = kind == NoteKind.SpeedUp ? speedUpColor : baseColor;
    }

    public void SetSize(float width, float height)
    {
        baseScale = new Vector3(width, height, 1f);
        transform.localScale = baseScale;
    }

    public void SetPosition(float x, float y)
    {
        transform.position = new Vector3(x, y, 0f);
    }

    public void PlayExit(Judgement grade)
    {
        KillTweens();
        IsExiting = true;

        // Unscaled: a Miss triggers hitstop, and the feedback for that Miss must not crawl
        // along with the slowed clock it just caused.
        Sequence seq = DOTween.Sequence().SetUpdate(true).OnComplete(onExitFinished);

        if (grade == Judgement.Miss)
        {
            body.color = palette.ColorOf(Judgement.Miss);
            seq.Append(body.DOFade(0f, missFadeDuration).SetEase(Ease.InQuad));
            return;
        }

        // Blown-out white first, grade colour second. The eye reads a brightness spike as impact
        // far more strongly than a hue change — but only if the spike is actually brighter than
        // what follows, and the grade colours are HDR at ~2.1.
        body.color = hitFlashColor;

        seq.Append(body.DOColor(palette.ColorOf(grade), hitPunchDuration));
        seq.Join(transform.DOScale(baseScale * (1f + hitPunch), hitPunchDuration).SetEase(Ease.OutBack));

        seq.Append(body.DOFade(0f, hitFadeDuration).SetEase(Ease.InQuad));
        seq.Join(transform.DOScale(baseScale * hitDissolveScale, hitFadeDuration).SetEase(Ease.OutQuad));
    }

    /// <summary>Aborts the exit without firing its completion — used when the chart is rebuilt.</summary>
    public void CancelExit()
    {
        KillTweens();
        IsExiting = false;
    }

    private void KillTweens()
    {
        // Kill by target rather than holding a Sequence reference: with tween recycling on,
        // a stored reference can point at a recycled tween belonging to something else.
        transform.DOKill();
        body.DOKill();
    }
}