using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputController : MonoBehaviour
{
    [SerializeField] private NoteController noteController;
    [Header("Demo")]
    [Tooltip("Plays the chart automatically. Use it to verify sync without input, " +
             "and to record a clean demo video.")]
    [SerializeField] private bool autoplay;

    [Tooltip("Autoplay cycles Perfect / Good / Miss instead of hitting everything perfectly, " +
             "so all three sets of feedback show up in one run. Turn off for a clean demo video.")]
    [SerializeField] private bool cycleJudgements = true;

    [SerializeField] private Button btnAuto;
    [SerializeField] private TMP_Text txtAuto;

    // Awake, not Start: GameController disables this component in its own Awake, and Unity defers
    // Start until a component is first enabled. On Start the AUTO button would be wired only once
    // the player pressed START — dead on the menu, with a stale ON/OFF label.
    private void Awake()
    {
        SetTextAuto();
        btnAuto.onClick.AddListener(() =>
        {
            autoplay = !autoplay;
            SetTextAuto();
        });
    }

    /// <summary>
    /// Called by GameController on every start, so a retry never inherits the previous run's
    /// autoplay state.
    /// </summary>
    public void SetAutoplay(bool on)
    {
        autoplay = on;
        SetTextAuto();
    }

    private void SetTextAuto()
    {
        txtAuto.text = autoplay ? "ON" : "OFF";
    }

    private void Update()
    {
        if (autoplay)
        {
            noteController.AutoHitDueNotes(cycleJudgements);
            return;
        }
        
        int touchCount = Input.touchCount;

        if (touchCount > 0)
        {
            // Every began touch, not just the first: Magic Tiles is a two-thumb game and
            // simultaneous taps on different lanes must all register.
            for (int i = 0; i < touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began && !OverUI(touch.fingerId))
                    JudgeAtScreenX(touch.position.x);
            }
        }
        else if (Input.GetMouseButtonDown(0) && !OverUI(PointerInputModule.kMouseLeftId))
        {
            // else, not a second if: on device Unity synthesises mouse events from touch,
            // which would judge the same tap twice.
            JudgeAtScreenX(Input.mousePosition.x);
        }
    }

    /// <summary>
    /// A lane is not an object — JudgeAtScreenX is pure screen-width maths, so the strip under
    /// every UI button is live gameplay surface. Legacy Input polls the OS directly and never
    /// sees the GraphicRaycaster, so a tap on a button fires onClick AND judges its lane unless
    /// the UI is excluded here. Per pointer id: the no-argument overload only tracks the last
    /// pointer, which is wrong the moment two thumbs are down.
    /// </summary>
    private static bool OverUI(int pointerId) =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);

    private void JudgeAtScreenX(float screenX)
    {
        int laneCount = noteController.LaneCount;
        int lane = Mathf.Clamp(
            Mathf.FloorToInt(screenX / Screen.width * laneCount),
            0, laneCount - 1);

        noteController.TryJudgeLane(lane);
    }
}