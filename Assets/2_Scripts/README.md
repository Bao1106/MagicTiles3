# Script Architecture — MVC + Observer

## Layers

```
Models/       Plain C# + ScriptableObject. State and data. No UnityEngine.UI, no View refs.
Views/        MonoBehaviour. Renders state, plays animation, forwards raw input. No game rules.
Controllers/  MonoBehaviour. Owns Models, applies rules, drives Views.
Core/         Cross-cutting services: GameEvents (observer hub), Conductor (music clock).
```

## Dependency rule

```
Controller ──reads/writes──► Model
Controller ──may call──────► View        (direct reference is fine, top-down)
View ───────raises─────────► GameEvents  (never calls a Controller directly)
View ───────listens to─────► GameEvents
Model ──────knows nothing about anything above it
```

One direction only. Anything travelling **upward** goes through `GameEvents`.
That is the whole observer layer — there is no bus, no registry, no reflection.

## Where things go

| Class | Folder | Kind |
|---|---|---|
| `GameEvents` | Core | static event hub |
| `Conductor` | Core | music clock service (`AudioSettings.dspTime`) |
| `GameState`, `Judgement`, `HitResult` | Models | enums / value types |
| `SongData` | Models | ScriptableObject (`.asset` lives in `1_Assets/Data/`) |
| `ScoreModel`, `SessionModel` | Models | plain C#, no MonoBehaviour |
| `GameController` | Controllers | composition root — creates models, wires views |
| `NoteController` | Controllers | note schedule, tile pool, judgement |
| `InputController` | Controllers | tap → lane index → judgement request |
| `AudioController` | Controllers | SFX, mixer snapshots |
| `TileView`, `LaneView`, `HudView`, `GameOverView` | Views | rendering only |
| `CameraShakeView`, `HitVfxView`, `PostFxView` | Views/Feedback | juice, all driven by `NoteJudged` |

## Two rules that keep this from rotting

**1. Do not give every entity an M, a V and a C.**
A tile is a **View only**. Its data lives in a `NoteData` array owned by `NoteController`.
30 tiles × 3 objects each is object soup, and the case study explicitly asks for KISS.

**2. Unsubscribe in `OnDisable`.**
`GameEvents` is static, so a subscription is a strong reference. A View that subscribes
in `OnEnable` and never unsubscribes leaks its entire GameObject graph across scene loads.

```csharp
private void OnEnable()  => GameEvents.ScoreChanged += HandleScoreChanged;
private void OnDisable() => GameEvents.ScoreChanged -= HandleScoreChanged;
```

## Composition root

`GameController` is the only place that news up Models and hands them to Controllers.
No singletons, no service locator, no `GameObject.Find`. If a class needs something,
it is passed in — either via `[SerializeField]` in the Inspector or from `GameController.Awake`.
