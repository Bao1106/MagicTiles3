public class ScoreModel
{
    /// <summary>Flat per-hit value required by the brief: Hit = +100, Miss = 0.</summary>
    public const int HitScore = 100;

    public int Score { get; private set; }
    public int Combo { get; private set; }
    public int BestCombo { get; private set; }
    public int HitCount { get; private set; }
    public int PerfectCount { get; private set; }

    /// <summary>
    /// Step multiplier: x2 at 10, x3 at 25, x4 at 50. Applied after the combo increments,
    /// so the very tap that reaches a milestone already earns the new rate — the reward
    /// lands on the same beat as the celebration.
    /// </summary>
    /// <summary>
    /// Combo values that step the multiplier. Public so audio escalation lands on the exact tap
    /// that changes the rate, instead of keeping a second copy of the thresholds.
    /// </summary>
    public static readonly int[] Milestones = { 10, 25, 50 };

    public static bool IsMilestone(int combo) => System.Array.IndexOf(Milestones, combo) >= 0;

    /// <summary>
    /// Step multiplier: x2 at 10, x3 at 25, x4 at 50. Applied after the combo increments, so the
    /// very tap that reaches a milestone already earns the new rate — the reward lands on the
    /// same beat as the celebration.
    /// </summary>
    public int Multiplier
    {
        get
        {
            int m = 1;
            foreach (int t in Milestones) if (Combo >= t) m++;
            return m;
        }
    }

    public void Reset()
    {
        Score = 0;
        Combo = 0;
        BestCombo = 0;
        HitCount = 0;
        PerfectCount = 0;
    }

    public void RegisterHit(bool isPerfect)
    {
        Combo++;
        if (Combo > BestCombo) BestCombo = Combo;

        HitCount++;
        if (isPerfect) PerfectCount++;

        Score += HitScore * Multiplier;
    }

    public void RegisterMiss()
    {
        Combo = 0;
    }
}