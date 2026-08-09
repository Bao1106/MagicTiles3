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
    [SerializeField] private float leadInSeconds = 2f;

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

    private void StartGame()
    {
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
            EndGame();
            return;
        }

        score.RegisterHit();
        GameEvents.RaiseScoreChanged(score.Score);
        GameEvents.RaiseComboChanged(score.Combo);
    }

    private void EndGame()
    {
        state = GameState.GameOver;
        conductor.Stop();
        inputController.enabled = false;
        GameEvents.RaiseGameOver(score.Score, score.BestCombo);
    }
}