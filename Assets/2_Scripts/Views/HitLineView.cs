using DG.Tweening;
using UnityEngine;

public class HitLineView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer line;
    [SerializeField] private JudgementPalette palette;

    [Tooltip("Idle colour of the line. HDR: values above 1 are what Bloom actually keys off. " +
             "SpriteRenderer.color in the Inspector is LDR and cannot exceed 1, so the resting " +
             "colour has to come from here instead.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color idleColor = new Color(0.13f, 0.89f, 1f);
    
    [Tooltip("Vertical scale multiplier at the peak of the pulse.")]
    [SerializeField] private float pulseScale = 2.5f;
    [SerializeField] private float pulseIn = 0.04f;
    [SerializeField] private float pulseOut = 0.12f;

    private Vector3 baseScale;
    private Color baseColor;

    private void Awake()
    {
        baseScale = transform.localScale;
        baseColor = idleColor;
        line.color = idleColor;
    }

    private void OnEnable() => GameEvents.NoteJudged += HandleNoteJudged;
    private void OnDisable() => GameEvents.NoteJudged -= HandleNoteJudged;

    private void HandleNoteJudged(HitResult result)
    {
        transform.DOKill();
        line.DOKill();
        transform.localScale = baseScale;

        Color flash = palette.ColorOf(result.Grade);

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(transform.DOScaleY(baseScale.y * pulseScale, pulseIn).SetEase(Ease.OutQuad));
        seq.Join(line.DOColor(flash, pulseIn));
        seq.Append(transform.DOScaleY(baseScale.y, pulseOut).SetEase(Ease.OutQuad));
        seq.Join(line.DOColor(baseColor, pulseOut));
    }
}