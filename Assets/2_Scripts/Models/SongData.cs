using UnityEngine;

[CreateAssetMenu(fileName = "SongData", menuName = "MagicTile/Song Data")]
public class SongData : ScriptableObject
{
    [Header("Audio")]
    [SerializeField]
    private AudioClip clip;

    [Header("Rhythm")]
    [Tooltip("Beats per minute of the track. Must be exact — an error of 0.5 BPM drifts " +
             "past the hit window before the song ends.")]
    [SerializeField, Min(1f)]
    private float bpm = 128f;

    [Tooltip("Time of the first beat, in seconds from the start of the audio file. " +
             "This is a property of the file, NOT device latency — that lives on Conductor.")]
    [SerializeField, Min(0f)]
    private float firstBeatOffset = 0f;

    [Tooltip("Beats of music that play before the first note. A chart starting at t=0 is " +
             "unhittable: the player gets no musical cue and the song has not established its " +
             "groove yet. 4 beats = one bar at 4/4.")]
    [SerializeField, Min(0)]
    private int leadInBeats = 4;
    
    [Tooltip("Notes per beat. 1 = quarter notes (calm), 2 = eighths (default), 4 = sixteenths (hard).")]
    [SerializeField, Range(1, 4)]
    private int subdivision = 2;

    [Tooltip("How many notes the chart contains. Use the Inspector button to fit it to the clip.")]
    [SerializeField, Min(1)]
    private int noteCount = 200;

    [Header("Feel")]
    [Tooltip("Seconds for a note to travel from the top of the screen to the hit line. " +
             "Resolution-independent, unlike a world-units-per-second speed. " +
             "Lower = faster and harder.")]
    [SerializeField, Range(0.4f, 3f)]
    private float approachTime = 1.2f;

    [Header("Determinism")]
    [Tooltip("Fixed seed keeps the chart identical every run: repeatable testing, " +
             "stable autoplay, reproducible demo recordings.")]
    [SerializeField]
    private int seed = 1337;

    public AudioClip Clip => clip;
    public float Bpm => bpm;
    public float FirstBeatOffset => firstBeatOffset;
    public int Subdivision => subdivision;
    public int NoteCount => noteCount;
    public float ApproachTime => approachTime;
    public int Seed => seed;

    /// <summary>Seconds between two consecutive notes.</summary>
    public float SecondsPerNote => 60f / bpm / subdivision;

    public int LeadInBeats => leadInBeats;

    /// <summary>
    /// Time of the first note, in seconds from the start of the audio file.
    /// </summary>
    public float ChartStartTime => firstBeatOffset + leadInBeats * (60f / bpm);
    public float ChartEndTime => ChartStartTime + (noteCount - 1) * SecondsPerNote;
}