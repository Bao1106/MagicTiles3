public struct NoteData
{
    /// <summary>
    /// When this note should be tapped, in seconds from song start.
    /// </summary>
    public float TargetTime;

    public int Lane;
    public bool Judged;
    public NoteKind Kind;

    /// <summary>
    /// World units per second, captured when the tile spawned. Kept per note so a mid-run speed
    /// change only touches tiles that have not appeared yet. One shared fall speed would move
    /// every tile already on screen the instant it changed — and move them further from the hit
    /// line, which reads as the opposite of speeding up.
    /// </summary>
    public float FallSpeed;
}
