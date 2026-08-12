using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JudgementPopupView : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image ring;
    [SerializeField] private ParticleSystem burst;
    [SerializeField] private JudgementPalette palette;

    [Header("Placement")]
    [SerializeField] private Camera worldCamera;
    [SerializeField, Range(0f, 1f)] private float viewportY = 0.72f;
    [SerializeField] private float edgePadding = 140f;

    [Header("Label — Forward Punch (Z-depth fake)")]
    [SerializeField] private float popDuration = 0.20f;
    [SerializeField] private float holdDuration = 0.08f;
    [SerializeField] private float fadeDuration = 0.20f;
    [SerializeField] private float riseDistance = 0f;

    [Header("Scale Curve — forward whoosh")]
    [Tooltip("Y=0: scale 0.3x | Y=1: scale 1.0x. A steeper initial slope creates a stronger whoosh.")]
    [SerializeField] private AnimationCurve popCurve = new AnimationCurve(
        new Keyframe(0f, 0f),       // 0.3x
        new Keyframe(0.15f, 1.15f), // overshoot
        new Keyframe(0.35f, 0.98f), // slower settle
        new Keyframe(0.50f, 1.0f),  // lock
        new Keyframe(1.0f, 1.0f)
    );

    [Header("Brightness Punch — glow as it approaches")]
    [SerializeField] private Color normalColor = new Color(1f, 0.97f, 0.86f);
    [SerializeField] private Color punchColor = Color.white;
    [SerializeField] private float punchDuration = 0.08f;

    [Header("Ring — expands from back to front")]
    [SerializeField] private float ringDuration = 0.55f;
    [SerializeField] private float ringStartScale = 0.15f;      // Smaller than the text = appears behind it
    [SerializeField] private float perfectRingScale = 3.8f;     // Larger than the text = appears in front
    [SerializeField] private float goodRingScale = 2.8f;

    private RectTransform rect;
    private RectTransform parentRect;
    private Camera uiCamera;
    private float anchorWorldY;
    private Sequence currentSeq;

    private void Awake()
    {
        rect = label.rectTransform;
        parentRect = (RectTransform)rect.parent;
        if (worldCamera == null) worldCamera = Camera.main;
        anchorWorldY = worldCamera.ViewportToWorldPoint(new Vector3(0.5f, viewportY, 10f)).y;

        Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
        uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (burst != null)
            burst.transform.localPosition = Vector3.zero;

        // Make sure the ring is behind the text in the hierarchy (rendered first = behind).
        if (ring != null)
            ring.rectTransform.SetAsFirstSibling();

        Clear();
    }

    private void OnEnable()
    {
        GameEvents.NoteJudged += HandleNoteJudged;
        GameEvents.GameStarted += Clear;
    }

    private void OnDisable()
    {
        GameEvents.NoteJudged -= HandleNoteJudged;
        GameEvents.GameStarted -= Clear;
        Clear();
    }

    private void HandleNoteJudged(HitResult result)
    {
        Vector3 world = new Vector3(0f, anchorWorldY, 0f);
        PlayLabel(result.Grade, world);

        if (result.Grade == Judgement.Miss)
        {
            KillRing();
            SetRingAlpha(0f);
            return;
        }

        bool perfect = result.Grade == Judgement.Perfect;
        Color gradeColor = palette.ColorOf(result.Grade);

        PlayRing(
            perfect ? perfectRingScale : goodRingScale,
            gradeColor.a,
            gradeColor
        );

        if (burst != null && result.Grade != Judgement.Miss)
        {
            // ===== SET PARTICLE COLOR =====
            var main = burst.main;

            // Use the grade color with full alpha.
            main.startColor = new ParticleSystem.MinMaxGradient(gradeColor);

            // If you want a stronger glow for PERFECT, multiply the color by an HDR intensity.
            // main.startColor = new ParticleSystem.MinMaxGradient(gradeColor * 1.5f);

            if (perfect)
            {
                burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                burst.Play(true);
            }
        }
    }

    private void PlayLabel(Judgement grade, Vector3 world)
    {
        if (currentSeq != null && currentSeq.IsActive())
            currentSeq.Kill(true);

        rect.DOKill();
        DOTween.Kill(this);
        KillRing();

        Vector2 p = WorldToCanvas(world);
        p.x = 0f;

        float half = parentRect.rect.width * 0.5f - edgePadding;
        p.x = Mathf.Clamp(p.x, -half, half);

        // ===== GET COLOR FROM PALETTE =====
        Color gradeColor = palette.ColorOf(grade);

        label.SetText(palette.LabelOf(grade));
        label.color = gradeColor;        // Use the grade color instead of the hardcoded normal color.
        label.alpha = 0f;
        rect.anchoredPosition = p;
        rect.localScale = Vector3.one * 0.3f;

        // Reset ring.
        ring.rectTransform.anchoredPosition = Vector2.zero;
        ring.rectTransform.localScale = Vector3.one * ringStartScale;
        SetRingAlpha(0f);

        currentSeq = DOTween.Sequence().SetUpdate(true).SetId(this);

        // 1. Scale whoosh.
        currentSeq.Append(rect.DOScale(1f, popDuration).SetEase(popCurve));

        // 2. Fade in.
        currentSeq.Join(
            DOTween.To(() => label.alpha, a => label.alpha = a, 1f, popDuration * 0.25f)
                .SetEase(Ease.OutQuad)
        );

        // 3. Brightness punch: brighten first, then return to the grade color.
        // Create the punch color by blending the grade color with white (70% white).
        Color punchTarget = Color.Lerp(gradeColor, Color.white, 0.7f);

        currentSeq.Join(
            label.DOColor(punchTarget, punchDuration).SetEase(Ease.OutQuad)
        );

        currentSeq.Join(
            label.DOColor(gradeColor, popDuration - punchDuration)
                .SetDelay(punchDuration)
                .SetEase(Ease.InOutQuad)
        );

        // 4. Hold, then fade out.
        currentSeq.AppendInterval(holdDuration);

        currentSeq.Append(
            DOTween.To(() => label.alpha, a => label.alpha = a, 0f, fadeDuration)
                .SetEase(Ease.InQuad)
        );

        currentSeq.Join(
            rect.DOScale(0.85f, fadeDuration).SetEase(Ease.InQuad)
        );
    }

    private void PlayRing(float peakScale, float startAlpha, Color color)
    {
        ring.color = color;
        ring.rectTransform.anchoredPosition = Vector2.zero;
        ring.rectTransform.localScale = Vector3.one * ringStartScale;
        SetRingAlpha(startAlpha);

        // The ring expands from behind the text (small) to the front (large).
        // A longer duration than the text creates a clearer parallax depth effect.
        ring.rectTransform.DOScale(peakScale, ringDuration)
            .SetEase(Ease.OutQuint)
            .SetUpdate(true);

        ring.DOFade(0f, ringDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private void Clear()
    {
        if (currentSeq != null && currentSeq.IsActive())
            currentSeq.Kill(true);

        rect.DOKill();
        DOTween.Kill(this);
        KillRing();

        label.alpha = 0f;
        label.color = normalColor;
        SetRingAlpha(0f);
        rect.localScale = Vector3.one * 0.3f;

        if (burst != null)
            burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void KillRing()
    {
        ring.rectTransform.DOKill();
        ring.DOKill();
    }

    private void SetRingAlpha(float a)
    {
        Color c = ring.color;
        c.a = a;
        ring.color = c;
    }

    private Vector2 WorldToCanvas(Vector3 world)
    {
        Vector3 screen = worldCamera.WorldToScreenPoint(world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screen,
            uiCamera,
            out Vector2 local
        );

        return local;
    }
}

