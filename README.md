# Magic Tiles 3 (Simplified)

A four-lane rhythm game for the Uplive Unity home test. Tiles fall toward a hit line, you tap
the lane one lands in, and a single miss ends the run.

Unity **2022.3.55f1**, URP **14.0.11**. Built portrait for Android, runs the same in the Editor.

## How to Run

1. Open the project in Unity 2022.3.55f1.
2. Open `Assets/3_Scenes/GameScene.unity`.
3. Set the Game view to a portrait aspect — 1080×1920 or 9:16. Layout is derived from the
   viewport so it adapts to other ratios, but portrait is what it's tuned for.
4. Press Play.

Click a lane on desktop, tap on device. Multi-touch works, so two thumbs are fine.

There's an **AUTO** button in the top-left corner — tap it for a hands-free run, which is what the
demo recording uses. Every start and every retry turns it back off, so a run never begins in
autoplay by accident.

`Assets/3_Scenes/OptimizationScene.unity` is the isolated scene for Task 2 — see
[OPTIMIZATION.md](OPTIMIZATION.md).

## Design Choices

### Timing runs on the audio clock

Everything is driven by `AudioSettings.dspTime`. The music starts with `PlayScheduled` at a known
DSP timestamp and `Conductor.SongPosition` is just the difference. `Time.deltaTime` accumulates
error and `AudioSource.time` only moves when the audio thread does — either one drifts away from
the music over 87 seconds, and in a rhythm game that drift *is* the bug.

`dspTime` only ticks once per audio buffer, so between ticks the Conductor interpolates with
unscaled delta time, clamped to one buffer length. Skip the clamp and the interpolation runs past
the next real tick, which then snaps backwards and stutters every tile on screen.

### Tiles are positioned, not moved

```
y = hitLineY + (TargetTime - songPosition) * fallSpeed
```

Position is a pure function of song time. Nothing accumulates, so nothing drifts, and a frame
spike corrects itself on the next frame instead of leaving every tile permanently offset. One
loop in `NoteController` moves everything — `TileView` has no `Update` at all.

### Layout comes from the viewport

Lane centres, the hit line and the spawn line all come from `ViewportToWorldPoint`.
`InputController` converts a tap with `screenX / Screen.width * laneCount`, so both sides are
describing the same screen fractions and can't disagree on any aspect ratio.

The hit line *sprite* is a case worth calling out: `NoteController` writes its Y position rather
than trusting where I dragged it. I had it sitting one world unit above where judgement actually
happened, which worked out to exactly one hit window — tiles looked hittable and weren't. Letting
the controller own the graphic killed the whole class of bug.

### MVC per module, not per entity

Models are plain C# and ScriptableObjects, views render and animate, controllers own the models
and the rules. Anything travelling upward goes through `GameEvents`, a static event hub — views
raise and listen, they never call a controller.

A tile is a **view only**; its data lives in a `NoteData` array owned by `NoteController`. Giving
each of 30 tiles its own model and controller would be object soup for nothing. Static events
leak if you forget them, so every subscriber unsubscribes in `OnDisable`, and `GameEvents` resets
itself in a `[RuntimeInitializeOnLoadMethod]` so nothing survives a Play Mode restart with Domain
Reload disabled. Full breakdown in [Assets/2_Scripts/README.md](Assets/2_Scripts/README.md).

### Game feel

Judgement picks the note **closest in time**, not the lowest on screen. When two notes are close
together the lowest may already be past its window, and a player correctly aiming at the next one
would get punished for it.

Windows are 70 ms for Perfect, 180 ms to register at all. Choice reaction time across four lanes
is roughly 400 ms, so tightening much further stops testing rhythm and starts testing reflexes.

Tile height and approach time are linked: a tile taller than `2 × hitWindow` of travel is visibly
across the line while being mechanically dead. The invariant is
`tileHeightRatio × approachTime ≤ 2 × hitWindow` — currently 0.345 against 0.36, so it just holds.

The chart starts four beats in, not at t=0. My first build died instantly because note 0 sat at
song time 0 and got missed before the player heard anything.

A miss **freezes** rather than cuts. The song clock stops so every tile hangs in place, but the
music keeps playing through a lowpass on the mixer, and the result panel waits a second. Raising
it on the same frame buried the shake, the red vignette and the tile falling away — all the
feedback I'd just paid for.

Hitstop is miss-only. `dspTime` ignores `timeScale`, so slowing the game on a hit would leave
tiles falling at full speed while everything else crawled. Every tween is unscaled for the same
reason.

### Scoring

A hit is +100 as specified. On top of that the combo multiplier steps to ×2 at 10, ×3 at 25 and
×4 at 50 — the one deliberate deviation. With a flat rate and instant death there's no reason to
care whether your streak is 4 or 40.

Perfect and Good pay the same, which means the only reward for a Perfect is how it looks. That's
why Perfect gets the ring and the particle burst and Good doesn't.

### Chart generation

The chart is generated from `SongData` (BPM, first-beat offset, subdivision, note count) with a
fixed seed, so every run is identical — repeatable testing, stable autoplay, and a demo I can
re-record without the difficulty shifting under me. The one authoring rule is that a note never
repeats the previous lane, because on a phone that reads as one tap instead of two.

BPM and first-beat offset were measured from the mp3 rather than eyeballed: 128.000 BPM, first
beat at 0.026 s.

## AI Usage

All the audio is AI-generated. I used ChatGPT to work out what the track actually needed — tempo,
length, and a mood that suits a four-lane tile game — then generated it in **Suno**. The five sound
effects are **ElevenLabs** sound-effect generations: hit, miss, milestone, game over, victory.

The BPM wasn't taken on trust from the generator. `SongData` needs an exact figure — half a beat of
error drifts past the hit window before an 87-second song is over — so instead of tapping it out by
ear I had Claude parse the mp3 frame headers and use each frame's `part2_3_length` as a rough onset
signal (more bits spent on a frame means more happening in it), then autocorrelate that. 128.000 BPM,
first beat at 0.026 s. Both went straight into `SongData` and the whole chart is built on them.

The other place AI earned its keep was two bugs where the symptom pointed nowhere near the cause.

The first killed the run about a beat after Play, with every tile frozen mid-fall — which reads like
a loop or rendering fault. It wasn't. `firstBeatOffset` was 0, so note 0 sat at song time 0.000 and
was missed before anyone could reach it. That miss ended the run, `Conductor.Stop()` cleared
`IsRunning`, and `NoteController.Update` returns early on that — hence the frozen tiles. The
four-beat lead-in is the fix.

The second was tiles that wouldn't take a tap. The hit line sprite sat one world unit above the line
judgement actually used, and one unit over the fall speed worked out to 0.120 s — exactly the hit
window I had set at the time. Only the late half of a tile's pass over the line was hittable, so it
looked like broken input. `NoteController` now writes that sprite's position, so the graphic and the
judgement can't drift apart again.

The architecture was my call: MVC per module, a static `GameEvents` hub, tiles as views over one
`NoteData` array owned by `NoteController`. I set that up front, had Claude write most of the
implementation against it, and read back every file.

The reading is the part worth mentioning. It kept adding things nobody asked for — combo-scaled
ring expansion on the judgement popup, and a weaker ring for Good hits, neither of which was in
the spec I'd written. Both came back out. Wherever I hadn't decided the behaviour myself, what I
got was a guess with a confident comment on top.

## Asset Attributions

| Asset | Source |
|---|---|
| DOTween (free) | Demigiant — dotween.demigiant.com |
| Procedural UI Image | Josh H. — Unity Asset Store, free |
| Fredoka | Google Fonts, SIL Open Font License |
| Particle prefab, materials, textures | Supplied with the case study (`UnOptimized.unitypackage`) |
| `song_case_study_1.mp3` | Generated with Suno — see AI Usage |
| SFX (`sfx_hit`, `sfx_miss`, `sfx_milestone`, `sfx_game_over`, `sfx_victory`) | Generated with ElevenLabs — see AI Usage |

Scripts, materials, scene, UI layout and VFX setup are mine.

## Known Issues

- **No latency calibration.** `Conductor.latencyOffset` exists but sits at 0. Android output
  latency ranges 40–150 ms by device, so on some phones judgement will feel a little early.
  A calibration screen is the right fix and isn't in scope here.
- **The chart follows the grid, not the song.** It lands on every beat but doesn't respond to
  build-ups or drops, so the difficulty curve is flat for all 87 seconds.
- **No pause, no menu, one song.** Play starts immediately and Retry restarts the same chart.
- **Only one result button is wired.** Retry works; the second button is decorative.
- **Stars need a full clear.** They're scored on Perfect ratio, and since one miss ends the run,
  you only ever see them after clearing the whole song.
