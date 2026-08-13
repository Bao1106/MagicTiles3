using UnityEngine;

/// <summary>Sparks at the lane that was just hit Perfect.</summary>
public class PerfectHitVfxView : MonoBehaviour
{
    [Tooltip("Looping with emission rate 0 — particles come from Emit() alone. One system " +
             "draws all its live particles in a single call, so overlapping hits stay at one " +
             "draw call instead of restarting anything.")]
    [SerializeField] private ParticleSystem spark;

    [SerializeField, Min(1)] private int particlesPerHit = 8;

    private ParticleSystem.EmitParams emitParams;

    private void Awake()
    {
        // Emit() takes world coordinates only when the system simulates in world space.
        emitParams.applyShapeToPosition = true;
    }

    private void OnEnable() => GameEvents.NoteJudged += HandleNoteJudged;
    private void OnDisable() => GameEvents.NoteJudged -= HandleNoteJudged;

    private void HandleNoteJudged(HitResult result)
    {
        if (result.Grade != Judgement.Perfect) return;

        emitParams.position = result.WorldPosition;
        spark.Emit(emitParams, particlesPerHit);
    }
}
