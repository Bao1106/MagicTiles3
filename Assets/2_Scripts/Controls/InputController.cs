using System;
using System.Collections.Generic;
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

    // Reused across taps: RaycastAll fills a caller-owned list, and a fresh list plus a fresh
    // PointerEventData on every tap is garbage twice a second for the whole song.
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>(8);
    private PointerEventData pointerData;

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
                if (touch.phase == TouchPhase.Began && !OverInteractiveUI(touch.position))
                    JudgeAtScreenX(touch.position.x);
            }
        }
        else if (Input.GetMouseButtonDown(0) && !OverInteractiveUI(Input.mousePosition))
        {
            // else, not a second if: on device Unity synthesises mouse events from touch,
            // which would judge the same tap twice.
            JudgeAtScreenX(Input.mousePosition.x);
        }
    }

    /// <summary>
    /// True only when the tap landed on something the player can actually press.
    ///
    /// A lane is not an object — JudgeAtScreenX is pure screen-width maths, so the strip under
    /// every UI button is live gameplay surface. Legacy Input polls the OS directly and never
    /// sees the GraphicRaycaster, so a tap on a button would fire onClick AND judge its lane
    /// unless the UI is excluded here.
    ///
    /// NOT EventSystem.IsPointerOverGameObject. That answers a different question — "is any
    /// raycast-target graphic under the pointer" — and this scene has a full-screen decorative
    /// Background with Raycast Target left on, under a canvas that carries a GraphicRaycaster.
    /// It therefore returns true on every pixel of the screen and swallows every single tap:
    /// the game becomes unplayable by hand while autoplay, which never reads Input, looks fine.
    /// Asking for a Selectable instead means decorative art can never gate gameplay input.
    /// </summary>
    private bool OverInteractiveUI(Vector2 screenPosition)
    {
        EventSystem events = EventSystem.current;
        if (events == null) return false;

        pointerData ??= new PointerEventData(events);
        pointerData.position = screenPosition;

        uiHits.Clear();
        events.RaycastAll(pointerData, uiHits);

        for (int i = 0; i < uiHits.Count; i++)
            if (uiHits[i].gameObject.GetComponentInParent<Selectable>() != null)
                return true;

        return false;
    }

    private void JudgeAtScreenX(float screenX)
    {
        int laneCount = noteController.LaneCount;
        int lane = Mathf.Clamp(
            Mathf.FloorToInt(screenX / Screen.width * laneCount),
            0, laneCount - 1);

        noteController.TryJudgeLane(lane);
    }
}