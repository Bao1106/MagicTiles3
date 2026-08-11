using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostFxView : MonoBehaviour
{
    [SerializeField] private Volume volume;

    [Header("Vignette")]
    [SerializeField] private Color missVignetteColor = new Color(1f, 0.11f, 0.15f);
    [SerializeField] private float missVignetteIntensity = 0.45f;

    [Header("Chromatic aberration")]
    [SerializeField] private float missChromaIntensity = 0.7f;

    [Header("Timing")]
    [SerializeField] private float attack = 0.06f;
    [SerializeField] private float release = 0.30f;

    [Header("Hitstop")]
    [Tooltip("Miss only. Hit must never use hitstop: dspTime ignores timeScale, so tiles would " +
             "keep falling at full speed while everything else crawls.")]
    [SerializeField, Range(0.01f, 0.5f)] private float hitstopScale = 0.05f;
    [SerializeField] private float hitstopDuration = 0.08f;

    private Vignette vignette;
    private ChromaticAberration chroma;

    private void Awake()
    {
        // volume.profile returns a runtime clone. volume.sharedProfile is the asset on disk —
        // writing to that in the editor permanently bakes a red vignette into the file.
        volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out chroma);

        SetVignette(0f);
        SetChroma(0f);
    }

    private void OnEnable() => GameEvents.NoteJudged += HandleNoteJudged;

    private void OnDisable()
    {
        GameEvents.NoteJudged -= HandleNoteJudged;
        DOTween.Kill(this);
        Time.timeScale = 1f;              // never leave the game frozen behind us
    }

    private void HandleNoteJudged(HitResult result)
    {
        if (result.Grade != Judgement.Miss) return;

        vignette.color.value = missVignetteColor;

        DOTween.Kill(this);
        Sequence seq = DOTween.Sequence().SetUpdate(true).SetId(this);
        seq.Append(DOTween.To(GetVignette, SetVignette, missVignetteIntensity, attack));
        seq.Join(DOTween.To(GetChroma, SetChroma, missChromaIntensity, attack));
        seq.Append(DOTween.To(GetVignette, SetVignette, 0f, release).SetEase(Ease.OutQuad));
        seq.Join(DOTween.To(GetChroma, SetChroma, 0f, release * 0.8f).SetEase(Ease.OutQuad));

        StartCoroutine(HitStop());
    }

    private IEnumerator HitStop()
    {
        Time.timeScale = hitstopScale;
        yield return new WaitForSecondsRealtime(hitstopDuration);
        Time.timeScale = 1f;
    }

    private float GetVignette() => vignette.intensity.value;
    private void SetVignette(float v) => vignette.intensity.value = v;
    private float GetChroma() => chroma.intensity.value;
    private void SetChroma(float v) => chroma.intensity.value = v;
}