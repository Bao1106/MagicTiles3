using System.Collections;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private SongData song;

    [Header("Refs")]
    [SerializeField] private Conductor conductor;
    [SerializeField] private NoteController noteController;
    [SerializeField] private InputController inputController;

    [Tooltip("Silence before the first sample, so tiles are already falling when music starts.")]
    [SerializeField] private float leadInSeconds = 0.5f;

    [Header("Result pacing")]
    [Tooltip("Gap between the fatal miss and the result panel. The miss feedback (shake, red " +
             "vignette, tile falling away) needs this window to read; raising GameOver on the " +
             "same frame buries all of it under the panel.")]
    [SerializeField] private float lossResultDelay = 1.0f;
    [SerializeField] private float winResultDelay = 0.6f;

    private ScoreModel score;
    private GameState state;

    private void Awake()
    {
        score = new ScoreModel();

        // vSync overrides targetFrameRate. Leave it on and Android silently caps at 30.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    private void OnEnable() => GameEvents.NoteJudged += HandleNoteJudged;
    private void OnDisable() => GameEvents.NoteJudged -= HandleNoteJudged;

    private void Start() => StartGame();

    private void Update()
    {
        // One miss ends the run instantly, so still being alive past the last note's window
        // means every note was hit — that IS the win condition, no counting needed.
        if (state == GameState.Playing && conductor.SongPosition > song.ChartEndTime + 1f)
            EndGame(won: true);
    }

    /// <summary>Wired to the result panel's Retry button.</summary>
    public void RestartGame() => StartGame();

    private void StartGame()
    {
        StopAllCoroutines();          // a queued result announcement must not fire into the new run

        score.Reset();
        state = GameState.Playing;

        noteController.BuildChart(song);
        inputController.enabled = true;
        conductor.Play(song.Clip, leadInSeconds);

        GameEvents.RaiseScoreChanged(score.Score);
        GameEvents.RaiseComboChanged(score.Combo);
        GameEvents.RaiseGameStarted();
    }

    private void HandleNoteJudged(HitResult result)
    {
        if (state != GameState.Playing) return;

        if (result.Grade == Judgement.Miss)
        {
            score.RegisterMiss();
            GameEvents.RaiseComboChanged(score.Combo);
            EndGame(won: false);
            return;
        }

        score.RegisterHit(result.Grade == Judgement.Perfect);
        GameEvents.RaiseScoreChanged(score.Score);
        GameEvents.RaiseComboChanged(score.Combo);
    }

    private void EndGame(bool won)
    {
        // The run ends NOW — state, input, and music all stop on this frame (the classic
        // Piano Tiles failure freeze). Only the announcement is deferred, so the miss
        // feedback gets its window and every GameOver listener stays in sync with the panel.
        state = GameState.GameOver;
        conductor.Stop();
        inputController.enabled = false;
        StartCoroutine(AnnounceResult(won));
    }

    private IEnumerator AnnounceResult(bool won)
    {
        // Realtime: the miss that brought us here also triggered hitstop (timeScale 0.05),
        // and this wait must not stretch along with the slowdown it caused.
        yield return new WaitForSecondsRealtime(won ? winResultDelay : lossResultDelay);
        GameEvents.RaiseGameOver(score.Score, score.BestCombo, won);
    }
}