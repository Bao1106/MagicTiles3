public class ScoreModel
{
    /// <summary>
    /// Flat per-hit value required by the brief: Hit = +100, Miss = 0.
    /// </summary>
    public const int HitScore = 100;

    public int Score { get; private set; }
    public int Combo { get; private set; }
    public int BestCombo { get; private set; }

    public void Reset()
    {
        Score = 0;
        Combo = 0;
        BestCombo = 0;
    }

    public void RegisterHit()
    {
        Combo++;
        if (Combo > BestCombo) 
            BestCombo = Combo;

        // Combo multiplier hooks in here in Milestone 2 (bonus feature).
        Score += HitScore;
    }

    public void RegisterMiss()
    {
        Combo = 0;
    }
}