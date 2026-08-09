using TMPro;
using UnityEngine;

public class HUDView : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;

    private void OnEnable()
    {
        GameEvents.ScoreChanged += HandleScoreChanged;
        GameEvents.ComboChanged += HandleComboChanged;
    }

    private void OnDisable()
    {
        GameEvents.ScoreChanged -= HandleScoreChanged;
        GameEvents.ComboChanged -= HandleComboChanged;
    }

    private void HandleScoreChanged(int score)
    {
        // SetText writes into TMP's internal char buffer. score.ToString() would allocate
        // a new string on every change, feeding the GC on a timing-critical path.
        scoreText.SetText("{0}", score);
    }

    private void HandleComboChanged(int combo)
    {
        comboText.gameObject.SetActive(combo > 1);
        if (combo > 1) comboText.SetText("x{0}", combo);
    }
}