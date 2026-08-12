using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class NoteController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Conductor conductor;
    [SerializeField] private TileView tilePrefab;
    [SerializeField] private Transform tileRoot;
    [SerializeField] private Camera gameCamera;

    [Header("Layout")]
    [SerializeField, Range(2, 6)] private int laneCount = 4;

    [Tooltip("Viewport Y of the hit line. 0 = bottom of screen, 1 = top.")]
    [SerializeField, Range(0f, 0.5f)] private float hitLineViewportY = 0.15f;

    [Tooltip("Viewport Y where tiles spawn. Above 1 so they enter from off-screen.")]
    [SerializeField] private float spawnViewportY = 1.15f;

    [Tooltip("Tile height as a fraction of the top-to-hit-line distance.")]
    [SerializeField, Range(0.05f, 0.4f)] private float tileHeightRatio = 0.18f;

    [Header("Timing windows (seconds)")]
    [Tooltip("Within this of the note time counts as Perfect.")]
    [SerializeField] private float perfectWindow = 0.05f;

    [Tooltip("Outside Perfect but within this counts as Good. Beyond it the note is missed.")]
    [SerializeField] private float hitWindow = 0.12f;

    [Tooltip("Optional visual for the hit line. NoteController positions it from hitLineViewportY " +
             "so the graphic can never drift away from where judgement actually happens.")]
    [SerializeField] private Transform hitLineVisual;

    [Header("Speed-up tiles")]
    [Tooltip("Notes before this index are always normal. The player needs a few plain taps " +
             "before a tile starts changing the rules.")]
    [SerializeField, Min(0)] private int firstSpeedTileIndex = 8;

    [Tooltip("Every Nth note after that one is a speed-up tile. A fixed interval rather than a " +
             "roll, so the chart stays identical run to run like the rest of the generator.")]
    [SerializeField, Min(2)] private int speedTileInterval = 16;

    [Tooltip("Added to the fall-speed multiplier on each speed tile hit. Permanent for the run: " +
             "the generated chart is otherwise flat for 87 seconds, and this is what gives it a " +
             "curve without touching the music.")]
    [SerializeField] private float speedStep = 0.15f;

    [Tooltip("Ceiling on the multiplier. At 2.0 the approach time halves to 0.75 s, which is " +
             "still above the ~0.4 s it takes to choose between four lanes.")]
    [SerializeField] private float speedMax = 2f;


    private readonly List<TileView> activeTiles = new List<TileView>(32);
    // Tiles that have been judged and are playing their exit animation. They are no longer
    // moved, but must not return to the pool until the animation finishes.
    private readonly List<TileView> exitingTiles = new List<TileView>(8);
    private float[] laneCenterX;
    private ObjectPool<TileView> pool;

    private NoteData[] notes;
    private int nextSpawnIndex;

    private float hitLineY;
    private float spawnY;
    private float approachTime;
    private float speedMultiplier = 1f;
    private float laneWidthWorld;
    private float tileHeight;

    private int cachedScreenWidth;
    private int cachedScreenHeight;

    /// <summary>Tile width as a fraction of its lane, leaving a visible gutter between lanes.</summary>
    private const float TileWidthFraction = 0.88f;

    public int LaneCount => laneCount;

    /// <summary>Approach time after speed tiles. Shorter means the tile has less screen to cross.</summary>
    private float CurrentApproach => approachTime / speedMultiplier;

    private float CurrentFallSpeed => (spawnY - hitLineY) / CurrentApproach;

    private void Awake()
    {
        if (gameCamera == null) gameCamera = Camera.main;

        laneCenterX = new float[laneCount];
        RecalculateLayout();

        pool = new ObjectPool<TileView>(
            createFunc: CreateTile,
            actionOnGet: t => t.gameObject.SetActive(true),
            actionOnRelease: t => t.gameObject.SetActive(false),
            actionOnDestroy: t => Destroy(t.gameObject),
            // collectionCheck catches double-release, the classic pool bug whose symptom
            // (a ghost tile in the wrong place) costs hours to trace. Cost is a Contains
            // over at most 16 entries.
            collectionCheck: true,
            defaultCapacity: 16,
            maxSize: 64);
    }

    private TileView CreateTile()
    {
        TileView tile = Instantiate(tilePrefab, tileRoot);
        tile.gameObject.SetActive(false);
        return tile;
    }

    /// <summary>
    /// Derives every world coordinate from the camera viewport, so lane positions always
    /// line up with the screen-fraction lane index that InputController computes.
    /// Hardcoded world X values drift apart from taps on any aspect ratio but the authored one.
    /// </summary>
    private void RecalculateLayout()
    {
        for (int i = 0; i < laneCount; i++)
        {
            float viewportX = (i + 0.5f) / laneCount;
            laneCenterX[i] = gameCamera.ViewportToWorldPoint(new Vector3(viewportX, 0f, 0f)).x;
        }

        float leftEdge = gameCamera.ViewportToWorldPoint(Vector3.zero).x;
        float rightEdge = gameCamera.ViewportToWorldPoint(new Vector3(1f, 0f, 0f)).x;
        laneWidthWorld = (rightEdge - leftEdge) / laneCount;

        hitLineY = gameCamera.ViewportToWorldPoint(new Vector3(0f, hitLineViewportY, 0f)).y;
        spawnY = gameCamera.ViewportToWorldPoint(new Vector3(0f, spawnViewportY, 0f)).y;
        tileHeight = (spawnY - hitLineY) * tileHeightRatio;

        cachedScreenWidth = Screen.width;
        cachedScreenHeight = Screen.height;

        if (hitLineVisual != null)
        {
            Vector3 p = hitLineVisual.position;
            hitLineVisual.position = new Vector3(p.x, hitLineY, p.z);
        }
    }

    /// <summary>
    /// Builds a deterministic chart. A fixed seed means identical playthroughs:
    /// repeatable testing, stable autoplay, reproducible demo recordings.
    /// </summary>
    public void BuildChart(SongData song)
    {
        approachTime = song.ApproachTime;
        speedMultiplier = 1f;

        float interval = 60f / song.Bpm / song.Subdivision;
        float chartStart = song.ChartStartTime;
        var rng = new System.Random(song.Seed);

        notes = new NoteData[song.NoteCount];
        int previousLane = -1;

        for (int i = 0; i < notes.Length; i++)
        {
            int lane;
            // Never repeat a lane back to back: on a phone that means lifting and dropping
            // the same finger in the same spot, which feels bad and reads as one tap.
            do { lane = rng.Next(laneCount); } while (lane == previousLane);
            previousLane = lane;

            notes[i].TargetTime = chartStart + i * interval;
            notes[i].Lane = lane;
            notes[i].Judged = false;
            notes[i].Kind = i >= firstSpeedTileIndex &&
                            (i - firstSpeedTileIndex) % speedTileInterval == 0
                ? NoteKind.SpeedUp
                : NoteKind.Normal;
        }

        nextSpawnIndex = 0;
        ReleaseAllTiles();
    }

    private void Update()
    {
        ReleaseFinishedExits();
        
        if (notes == null || !conductor.IsRunning) return;

        if (Screen.width != cachedScreenWidth || Screen.height != cachedScreenHeight)
            RecalculateLayout();

        float songPosition = conductor.SongPosition;

        SpawnDueTiles(songPosition);
        MoveAndExpireTiles(songPosition);
    }

    private void SpawnDueTiles(float songPosition)
    {
        // approachTime IS the lead time — a tile spawns exactly when it needs to start
        // falling to reach the hit line on its note time. Change the feel, spawning follows.
        while (nextSpawnIndex < notes.Length &&
               notes[nextSpawnIndex].TargetTime - songPosition <= CurrentApproach)
        {
            ref NoteData note = ref notes[nextSpawnIndex];

            // Locked in now, not read live: a speed tile hit while this one is mid-flight must
            // not change the trajectory the player is already reading.
            note.FallSpeed = CurrentFallSpeed;

            TileView tile = pool.Get();
            tile.ResetVisual();
            tile.SetKind(note.Kind);
            tile.NoteIndex = nextSpawnIndex;
            tile.SetSize(laneWidthWorld * TileWidthFraction, tileHeight);
            tile.SetPosition(laneCenterX[note.Lane], spawnY);
            activeTiles.Add(tile);
            nextSpawnIndex++;
        }
    }

    private void Judge(int activeIndex, Judgement grade, float deltaSeconds)
    {
        TileView tile = activeTiles[activeIndex];
        ref NoteData note = ref notes[tile.NoteIndex];
        note.Judged = true;

        if (note.Kind == NoteKind.SpeedUp && grade != Judgement.Miss)
            speedMultiplier = Mathf.Min(speedMultiplier + speedStep, speedMax);

        var result = new HitResult(grade, note.Lane, tile.transform.position, deltaSeconds);
        GameEvents.RaiseNoteJudged(in result);
        Despawn(activeIndex, grade);
    }
    
    private void MoveAndExpireTiles(float songPosition)
    {
        // Backwards so removing an element does not skip the next one.
        for (int i = activeTiles.Count - 1; i >= 0; i--)
        {
            TileView tile = activeTiles[i];
            ref NoteData note = ref notes[tile.NoteIndex];

            // Position is a pure function of song time — nothing accumulates, nothing drifts.
            float y = hitLineY + (note.TargetTime - songPosition) * note.FallSpeed;
            tile.SetPosition(laneCenterX[note.Lane], y);

            if (!note.Judged && songPosition - note.TargetTime > hitWindow)
                Judge(i, Judgement.Miss, songPosition - note.TargetTime);
        }
    }

    /// <summary>
    /// Judges a tap on one lane. Picks the note closest in time, not the lowest on screen:
    /// with two notes near each other the lowest may already be out of the window, which
    /// would punish a player who correctly aimed at the next one.
    /// </summary>
    public void TryJudgeLane(int lane)
    {
        if (notes == null || !conductor.IsRunning) return;

        float songPosition = conductor.SongPosition;
        int bestIndex = -1;
        float bestAbsDelta = float.MaxValue;

        for (int i = 0; i < activeTiles.Count; i++)
        {
            ref NoteData note = ref notes[activeTiles[i].NoteIndex];
            if (note.Judged || note.Lane != lane) continue;

            float absDelta = Mathf.Abs(songPosition - note.TargetTime);
            if (absDelta < bestAbsDelta)
            {
                bestAbsDelta = absDelta;
                bestIndex = i;
            }
        }

        if (bestIndex < 0 || bestAbsDelta > hitWindow)
        {
            GameEvents.RaiseEmptyTap(lane);
            return;
        }

        Judgement grade = bestAbsDelta <= perfectWindow ? Judgement.Perfect : Judgement.Good;
        Judge(bestIndex, grade, songPosition - notes[activeTiles[bestIndex].NoteIndex].TargetTime);
    }

    /// <param name="cycleJudgements">
    /// Taps late on every second note and skips every third, so one autoplay run shows the
    /// Good and Miss feedback too — otherwise only the Perfect path is ever visible.
    /// </param>
    public void AutoHitDueNotes(bool cycleJudgements = false)
    {
        if (notes == null || !conductor.IsRunning) return;

        float songPosition = conductor.SongPosition;

        // Backwards: Judge() removes the entry at activeIndex.
        for (int i = activeTiles.Count - 1; i >= 0; i--)
        {
            int noteIndex = activeTiles[i].NoteIndex;
            ref NoteData note = ref notes[noteIndex];
            if (note.Judged) continue;

            float delay = 0f;
            if (cycleJudgements)
            {
                // Chỉ xoay vòng Perfect ↔ Good (bỏ Miss)
                // Even index  → Perfect (tap đúng nhịp, delay = 0)
                // Odd index   → Good (tap trễ, nằm giữa Perfect window và hit window)
                if (noteIndex % 2 == 1)
                {
                    delay = (perfectWindow + hitWindow) * 0.5f;
                }
                // noteIndex % 2 == 0 → delay giữ nguyên 0f (Perfect)
            }

            if (songPosition < note.TargetTime + delay) continue;

            float deltaSeconds = songPosition - note.TargetTime;
            Judge(i, Mathf.Abs(deltaSeconds) <= perfectWindow ? Judgement.Perfect : Judgement.Good, deltaSeconds);
        }
    }
    
    private void Despawn(int activeIndex, Judgement grade)
    {
        TileView tile = activeTiles[activeIndex];
        activeTiles.RemoveAt(activeIndex);

        exitingTiles.Add(tile);
        tile.PlayExit(grade);
    }

    private void ReleaseFinishedExits()
    {
        for (int i = exitingTiles.Count - 1; i >= 0; i--)
        {
            if (exitingTiles[i].IsExiting) continue;

            pool.Release(exitingTiles[i]);
            exitingTiles.RemoveAt(i);
        }
    }
    
    private void ReleaseAllTiles()
    {
        for (int i = activeTiles.Count - 1; i >= 0; i--)
            pool.Release(activeTiles[i]);
        activeTiles.Clear();

        // A restart must not leave last run's tiles fading over the new chart.
        for (int i = exitingTiles.Count - 1; i >= 0; i--)
        {
            exitingTiles[i].CancelExit();
            pool.Release(exitingTiles[i]);
        }
        exitingTiles.Clear();
    }

    private void OnDrawGizmos()
    {
        // Makes the hit line and lane centres visible in the Scene view while tuning layout.
        if (!Application.isPlaying || laneCenterX == null) return;
        Gizmos.color = Color.cyan;
        foreach (float x in laneCenterX)
            Gizmos.DrawLine(new Vector3(x, hitLineY - 1f, 0f), new Vector3(x, hitLineY + 1f, 0f));
    }
}