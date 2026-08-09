using TMPro;
using UnityEngine;

public class GameResultView : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text bestComboText;

    private void Awake() => panel.SetActive(false);

    private void OnEnable() => GameEvents.GameOver += HandleGameOver;
    private void OnDisable() => GameEvents.GameOver -= HandleGameOver;

    private void HandleGameOver(int finalScore, int bestCombo)
    {
        panel.SetActive(true);
        finalScoreText.SetText("{0}", finalScore);
        bestComboText.SetText("{0}", bestCombo);
    }
}