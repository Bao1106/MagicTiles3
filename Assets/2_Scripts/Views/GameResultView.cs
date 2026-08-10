using TMPro;
using UnityEngine;

public class GameResultView : MonoBehaviour
{
    [SerializeField] private CanvasGroup cvg;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text bestComboText;

    private void Awake() => cvg.alpha = 0;

    private void OnEnable() => GameEvents.GameOver += HandleGameOver;
    private void OnDisable() => GameEvents.GameOver -= HandleGameOver;

    private void HandleGameOver(int finalScore, int bestCombo)
    {
        cvg.alpha = 1;
        finalScoreText.SetText("{0}", finalScore);
        bestComboText.SetText("{0}", bestCombo);
    }
}