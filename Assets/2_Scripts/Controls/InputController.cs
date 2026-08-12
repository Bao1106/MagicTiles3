using System;
using TMPro;
using UnityEngine;
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

    private void Start()
    {
        SetTextAuto();
        btnAuto.onClick.AddListener(() =>
        {
            autoplay = !autoplay;
            SetTextAuto();
        });
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
                if (touch.phase == TouchPhase.Began)
                    JudgeAtScreenX(touch.position.x);
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            // else, not a second if: on device Unity synthesises mouse events from touch,
            // which would judge the same tap twice.
            JudgeAtScreenX(Input.mousePosition.x);
        }
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