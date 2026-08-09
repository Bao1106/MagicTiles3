using UnityEngine;

public readonly struct HitResult
{
    public readonly Judgement Grade;
    public readonly int Lane;
    public readonly Vector3 WorldPosition;

    /// <summary>
    /// Tap time minus note time, in seconds. Positive = late, negative = early.
    /// </summary>
    public readonly float DeltaSeconds;

    public HitResult(Judgement grade, int lane, Vector3 worldPosition, float deltaSeconds)
    {
        Grade = grade;
        Lane = lane;
        WorldPosition = worldPosition;
        DeltaSeconds = deltaSeconds;
    }
}