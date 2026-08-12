using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

public class AudioController : MonoBehaviour
{
    [Header("Sources")]
    [Tooltip("Per-hit sounds. Its pitch rides the combo, so nothing that must stay in tune " +
             "may play through it.")]
    [SerializeField] private AudioSource sfxSource;

    [Tooltip("Fixed-pitch punctuation: milestone, game over, victory. Pitch belongs to the " +
             "source, so a stinger sharing sfxSource would be bent 40% sharp at combo 50.")]
    [SerializeField] private AudioSource sfxStingerSource;
    
    [Header("Clips")]
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip missClip;
    [SerializeField] private AudioClip milestoneClip;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip victoryClip;

    [Header("Mixer")]
    [SerializeField] private AudioMixerSnapshot normalSnapshot;
    [SerializeField] private AudioMixerSnapshot muffledSnapshot;
    [SerializeField] private float snapshotFade = 0.4f;

    [Header("Pitch escalation")]
    [Tooltip("Hit pitch = 1 + min(combo, cap) * step. The cheapest sense of escalation there " +
             "is: no extra assets, and the player hears their streak building.")]
    [SerializeField] private float pitchStep = 0.02f;
    [SerializeField] private int pitchCap = 20;

    private int combo;

    private void OnEnable()
    {
        GameEvents.NoteJudged += HandleNoteJudged;
        GameEvents.GameOver += HandleGameOver;
        GameEvents.GameStarted += HandleGameStarted;
    }

    private void OnDisable()
    {
        GameEvents.NoteJudged -= HandleNoteJudged;
        GameEvents.GameOver -= HandleGameOver;
        GameEvents.GameStarted -= HandleGameStarted;
    }

    private void HandleNoteJudged(HitResult result)
    {
        if (result.Grade == Judgement.Miss)
        {
            combo = 0;
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(missClip);
            return;
        }

        combo++;

        // Layered, not replaced: the hit still confirms the tap, the stinger marks the rate
        // change. Swapping one for the other would drop the confirmation on the loudest beat.
        if (ScoreModel.IsMilestone(combo)) sfxStingerSource.PlayOneShot(milestoneClip);

        sfxSource.pitch = 1f + Mathf.Min(combo, pitchCap) * pitchStep;
        sfxSource.PlayOneShot(hitClip);
    }

    private void HandleGameStarted()
    {
        combo = 0;
        sfxSource.pitch = 1f;
        normalSnapshot?.TransitionTo(0.1f);
    }

    private void HandleGameOver(int score, int bestCombo, bool won)
    {
        // The lowpass sits on the Music group only, so the music goes underwater while this
        // stinger stays crisp.
        muffledSnapshot?.TransitionTo(snapshotFade);
        sfxStingerSource.PlayOneShot(won ? victoryClip : gameOverClip);
    }
}