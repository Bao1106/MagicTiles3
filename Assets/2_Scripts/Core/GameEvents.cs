using System;
using UnityEngine;

public static class GameEvents
{
    public static event Action GameStarted;

    /// <summary>
    /// Final score, best combo, and whether the player cleared the whole chart.
    /// </summary>
    public static event Action<int, int, bool> GameOver;

    public static event Action<int> ScoreChanged;

    /// <summary>
    /// New consecutive-hit count. 0 means the combo just broke.
    /// </summary>
    public static event Action<int> ComboChanged;

    ///
    /// <summary>A note was judged — grade, lane, world position, timing error.
    /// </summary>
    public static event Action<HitResult> NoteJudged;

    /// <summary>
    /// Player tapped a lane with no note in range.
    /// </summary>
    public static event Action<int> EmptyTap;

    public static void RaiseGameStarted() => GameStarted?.Invoke();
    public static void RaiseGameOver(int finalScore, int bestCombo, bool won) => GameOver?.Invoke(finalScore, bestCombo, won);
    public static void RaiseScoreChanged(int score) => ScoreChanged?.Invoke(score);
    public static void RaiseComboChanged(int combo) => ComboChanged?.Invoke(combo);
    public static void RaiseNoteJudged(in HitResult result) => NoteJudged?.Invoke(result);
    public static void RaiseEmptyTap(int lane) => EmptyTap?.Invoke(lane);

    /// <summary>
    /// Static state survives Play Mode when Domain Reload is disabled, leaving stale
    /// Subscribers from the previous run pointing at destroyed objects.
    /// SubsystemRegistration runs before any scene loads, so this always clears first.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        GameStarted = null;
        GameOver = null;
        ScoreChanged = null;
        ComboChanged = null;
        NoteJudged = null;
        EmptyTap = null;
    }
}