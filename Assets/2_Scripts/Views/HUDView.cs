using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDView : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    [Header("Combo Text = Vertical Progress Fill")]
    [SerializeField] private TMP_Text comboBase;       // Outline dim (track)
    [SerializeField] private TMP_Text comboFill;       // Bright fill (masked)
    [SerializeField] private RectTransform fillMask;   // RectMask2D parent, anchor bottom
    [SerializeField] private TMP_Text milestoneLabel;
    [SerializeField] private ParticleSystem comboBurst;
    [SerializeField] private Image screenEdgeGlow;     // Optional

    [Header("Punch")]
    [SerializeField] private float scorePunch = 0.12f;
    [SerializeField] private float comboPunch = 0.25f;
    [SerializeField] private float punchDuration = 0.15f;

    [Header("Milestones")]
    [SerializeField] private int[] milestones = { 10, 25, 50 };
    [SerializeField] private Color[] milestoneColors =
    {
        new (0.08f, 0.90f, 0.96f),  // Cyan  x10
        new (1f, 0.84f, 0f),        // Gold  x25
        new (1f, 0.42f, 0.21f)      // Orange x50
    };
    [SerializeField] private string[] milestoneLabels = { "NICE!", "GREAT!", "UNSTOPPABLE!" };

    [Header("Fill Tween")]
    [SerializeField] private float fillDuration = 0.15f;
    [SerializeField] private float milestoneFlashDuration = 0.35f;

    [Header("Milestone Label")]
    [SerializeField] private float labelPopDuration = 0.18f;
    [SerializeField] private float labelFadeDuration = 0.35f;
    [SerializeField] private float labelRiseDistance = 25f;

    private Color baseComboColor;
    private Sequence currentLabelSeq;
    private int lastCombo = 0;
    private float textHeight;

    private void Awake()
    {
        baseComboColor = comboBase.color;
        textHeight = comboBase.rectTransform.rect.height;
        ClearEffects();
    }

    private void OnEnable()
    {
        GameEvents.ScoreChanged += HandleScoreChanged;
        GameEvents.ComboChanged += HandleComboChanged;
    }

    private void OnDisable()
    {
        GameEvents.ScoreChanged -= HandleScoreChanged;
        GameEvents.ComboChanged -= HandleComboChanged;
        KillAllTweens();
    }

    private void HandleScoreChanged(int score)
    {
        scoreText.SetText("{0}", score);
        scoreText.rectTransform.DOKill(true);
        scoreText.rectTransform
                 .DOPunchScale(Vector3.one * scorePunch, punchDuration, vibrato: 1)
                 .SetUpdate(true);
    }

    private void HandleComboChanged(int combo)
    {
        bool visible = combo > 1;
        comboBase.gameObject.SetActive(visible);
        comboFill.gameObject.SetActive(visible);
        fillMask.gameObject.SetActive(visible);

        if (!visible)
        {
            KillAllTweens();
            ClearEffects();
            lastCombo = 0;
            return;
        }

        // Update both texts
        comboBase.SetText("x{0}", combo);
        comboFill.SetText("x{0}", combo);

        // Determine tier
        int milestoneIndex = -1;
        Color color = baseComboColor;

        for (int i = 0; i < milestones.Length; i++)
        {
            if (combo == milestones[i]) milestoneIndex = i;
            if (combo >= milestones[i] && i < milestoneColors.Length)
                color = milestoneColors[i];
        }

        // Set fill color + glow
        comboFill.color = color;
        comboFill.fontMaterial.SetColor(ShaderUtilities.ID_GlowColor, color);

        // Calculate fill height
        float fillPct = CalculateFillPercent(combo);
        float targetHeight = textHeight * fillPct;

        bool isMilestone = milestoneIndex >= 0;

        if (isMilestone)
        {
            // ===== MILESTONE: flash full then reset =====
            fillMask.DOKill();

            // Tween to full height
            fillMask.DOSizeDelta(new Vector2(fillMask.sizeDelta.x, textHeight), 0.08f)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        // Flash bright white
                        comboFill.DOColor(Color.white, 0.06f)
                                 .SetLoops(2, LoopType.Yoyo)
                                 .SetUpdate(true);

                        // Reset after flash
                        DOVirtual.DelayedCall(milestoneFlashDuration, () =>
                        {
                            fillMask.DOSizeDelta(new Vector2(fillMask.sizeDelta.x, 0f), 0.25f)
                                    .SetEase(Ease.InOutQuad)
                                    .SetUpdate(true);
                        }, true);
                    });

            // Combo pop (both layers)
            comboBase.rectTransform.DOKill(true);
            comboBase.rectTransform
                     .DOPunchScale(Vector3.one * (comboPunch * 2.2f), punchDuration * 1.3f, vibrato: 2)
                     .SetUpdate(true);
            comboFill.rectTransform
                     .DOPunchScale(Vector3.one * (comboPunch * 2.2f), punchDuration * 1.3f, vibrato: 2)
                     .SetUpdate(true);

            PlayMilestoneLabel(milestoneLabels[milestoneIndex], color);
            PlayBurst(color);
            PlayEdgeGlow(color);
        }
        else
        {
            // ===== NORMAL HIT: fill up =====
            fillMask.DOKill();
            fillMask.DOSizeDelta(new Vector2(fillMask.sizeDelta.x, targetHeight), fillDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);

            float tickPunch = (lastCombo == 0) ? comboPunch : comboPunch * 0.35f;
            comboBase.rectTransform.DOKill(true);
            comboBase.rectTransform
                     .DOPunchScale(Vector3.one * tickPunch, punchDuration, vibrato: 1)
                     .SetUpdate(true);
            comboFill.rectTransform
                     .DOPunchScale(Vector3.one * tickPunch, punchDuration, vibrato: 1)
                     .SetUpdate(true);

            if (combo % 5 == 0) PlayBurst(color, 0.6f);
        }

        lastCombo = combo;
    }

    private float CalculateFillPercent(int combo)
    {
        int prev = 0;
        int next = milestones[0];

        for (int i = 0; i < milestones.Length; i++)
        {
            if (combo >= milestones[i])
            {
                prev = milestones[i];
                next = (i + 1 < milestones.Length) ? milestones[i + 1] : milestones[i] + 25;
            }
            else
            {
                next = milestones[i];
                break;
            }
        }

        return Mathf.Clamp01((float)(combo - prev) / (next - prev));
    }

    // ==================== VFX ====================

    private void PlayMilestoneLabel(string text, Color color)
    {
        if (milestoneLabel == null) return;

        if (currentLabelSeq != null && currentLabelSeq.IsActive())
            currentLabelSeq.Kill(true);

        milestoneLabel.rectTransform.DOKill();
        DOTween.Kill(milestoneLabel);

        milestoneLabel.SetText(text);
        milestoneLabel.color = color;
        milestoneLabel.alpha = 0f;
        milestoneLabel.rectTransform.localScale = Vector3.one * 0.6f;
        milestoneLabel.rectTransform.anchoredPosition = Vector2.zero;

        currentLabelSeq = DOTween.Sequence().SetUpdate(true);

        currentLabelSeq.Append(
            milestoneLabel.rectTransform.DOScale(1.1f, labelPopDuration)
                .SetEase(Ease.OutBack, 2.5f)
        );
        currentLabelSeq.Join(
            DOTween.To(() => milestoneLabel.alpha, a => milestoneLabel.alpha = a, 1f, labelPopDuration * 0.5f)
                .SetEase(Ease.OutQuad)
        );

        currentLabelSeq.Append(
            milestoneLabel.rectTransform.DOAnchorPosY(labelRiseDistance, labelFadeDuration)
                .SetEase(Ease.OutQuint)
        );
        currentLabelSeq.Join(
            DOTween.To(() => milestoneLabel.alpha, a => milestoneLabel.alpha = a, 0f, labelFadeDuration)
                .SetEase(Ease.InQuad)
        );
        currentLabelSeq.Join(
            milestoneLabel.rectTransform.DOScale(0.9f, labelFadeDuration)
                .SetEase(Ease.InQuad)
        );
    }

    private void PlayBurst(Color color, float intensity = 1.5f)
    {
        if (comboBurst == null) return;

        var main = comboBurst.main;
        main.startColor = new ParticleSystem.MinMaxGradient(color * intensity);

        if (comboBurst.isPlaying)
            comboBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        comboBurst.Play(true);
        comboBurst.transform.localPosition = Vector3.zero;

        var renderer = comboBurst.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = "UI";
            renderer.sortingOrder = 100;
        }
    }

    private void PlayEdgeGlow(Color color)
    {
        if (screenEdgeGlow == null) return;
        screenEdgeGlow.color = color;
        screenEdgeGlow.DOKill();
        SetImageAlpha(screenEdgeGlow, 0.6f);
        screenEdgeGlow.DOFade(0f, 0.4f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    // ==================== CLEANUP ====================

    private void ClearEffects()
    {
        fillMask.sizeDelta = new Vector2(fillMask.sizeDelta.x, 0f);

        if (milestoneLabel != null)
        {
            milestoneLabel.alpha = 0f;
            milestoneLabel.rectTransform.anchoredPosition = Vector2.zero;
            milestoneLabel.rectTransform.localScale = Vector3.one;
        }

        if (comboBurst != null)
            comboBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (screenEdgeGlow != null)
            SetImageAlpha(screenEdgeGlow, 0f);
    }

    private void KillAllTweens()
    {
        if (currentLabelSeq != null && currentLabelSeq.IsActive())
            currentLabelSeq.Kill(true);

        if (milestoneLabel != null)
        {
            milestoneLabel.rectTransform.DOKill();
            DOTween.Kill(milestoneLabel);
        }

        fillMask.DOKill();
        comboBase.rectTransform.DOKill(true);
        comboFill.rectTransform.DOKill(true);
        scoreText.rectTransform.DOKill(true);

        if (screenEdgeGlow != null) screenEdgeGlow.DOKill();
    }

    private void SetImageAlpha(Image img, float a)
    {
        Color c = img.color; c.a = a; img.color = c;
    }
}