using DG.Tweening;
using UnityEngine;

public class CameraShakeView : MonoBehaviour
{
    [Header("Amplitude (world units)")]
    [SerializeField] private float perfectStrength = 0.02f;
    [SerializeField] private float missStrength = 0.15f;

    [Header("Duration (seconds)")]
    [SerializeField] private float perfectDuration = 0.05f;
    [SerializeField] private float missDuration = 0.25f;

    [SerializeField] private int vibrato = 18;

    private Vector3 basePosition;
    private TweenCallback restorePosition;

    private void Awake()
    {
        basePosition = transform.localPosition;
        restorePosition = () => transform.localPosition = basePosition;
    }

    private void OnEnable() => GameEvents.NoteJudged += HandleNoteJudged;

    private void OnDisable()
    {
        GameEvents.NoteJudged -= HandleNoteJudged;
        transform.DOKill();
        transform.localPosition = basePosition;
    }

    private void HandleNoteJudged(HitResult result)
    {
        switch (result.Grade)
        {
            case Judgement.Good: return;                                  // no shake, on purpose
            case Judgement.Perfect: Shake(perfectStrength, perfectDuration); break;
            default: Shake(missStrength, missDuration); break;
        }
    }

    /// <summary>Public so combo milestones can borrow it in M2.4.</summary>
    public void Shake(float strength, float duration)
    {
        transform.DOKill();
        transform.localPosition = basePosition;

        transform.DOShakePosition(duration, strength, vibrato, 90f, false, true)
            .SetUpdate(true)
            .OnComplete(restorePosition);
    }
}