using DG.Tweening;
using TMPro;
using UnityEngine;

public class GameResultView : MonoBehaviour
{
    [SerializeField] private CanvasGroup cvg;
    [SerializeField] private RectTransform sheet;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text bestComboText;
    [SerializeField] private GameObject[] stars;

    [Header("Copy")]
    [SerializeField] private string winTitle = "SONG CLEAR!";
    [SerializeField] private string loseTitle = "GAME OVER";

    [Header("Timing")]
    [SerializeField] private float backdropFade = 0.20f;
    [SerializeField] private float sheetSlide = 0.28f;
    [SerializeField] private float slideDistance = 60f;
    [SerializeField] private float countDuration = 0.7f;
    [SerializeField] private float starInterval = 0.12f;

    private int judgedCount;
    private int perfectCount;
    private Vector2 sheetHome;

    private void Awake()
    {
        sheetHome = sheet.anchoredPosition;
        Hide();
    }

    private void OnEnable()
    {
        GameEvents.NoteJudged += HandleNoteJudged;
        GameEvents.GameOver += HandleGameOver;
        GameEvents.GameStarted += HandleGameStarted;
    }

    private void OnDisable()
    {
        GameEvents.NoteJudged -= HandleNoteJudged;
        GameEvents.GameOver -= HandleGameOver;
        GameEvents.GameStarted -= HandleGameStarted;
        DOTween.Kill(this);
    }

    private void HandleNoteJudged(HitResult result)
    {
        judgedCount++;
        if (result.Grade == Judgement.Perfect) perfectCount++;
    }

    private void HandleGameStarted()
    {
        judgedCount = 0;
        perfectCount = 0;
        DOTween.Kill(this);
        Hide();
    }

    private void Hide()
    {
        cvg.alpha = 0f;
        cvg.interactable = false;
        cvg.blocksRaycasts = false;
        gameObject.SetActive(true);          // stays active so OnEnable subscriptions hold
    }

    private void HandleGameOver(int finalScore, int bestCombo, bool won)
    {
        titleText.SetText(won ? winTitle : loseTitle);
        bestComboText.SetText("x{0}", bestCombo);

        int starCount = 0;
        if (won && judgedCount > 0)
        {
            float perfectRatio = (float)perfectCount / judgedCount;
            starCount = perfectRatio >= 0.8f ? 3 : perfectRatio >= 0.5f ? 2 : 1;
        }

        for (int i = 0; i < stars.Length; i++)
            stars[i].transform.localScale = Vector3.zero;

        cvg.interactable = true;
        cvg.blocksRaycasts = true;
        sheet.anchoredPosition = sheetHome - new Vector2(0f, slideDistance);

        Sequence seq = DOTween.Sequence().SetUpdate(true).SetId(this);

        seq.Append(cvg.DOFade(1f, backdropFade));
        seq.Join(sheet.DOAnchorPos(sheetHome, sheetSlide).SetEase(Ease.OutCubic));

        // Counting up is the cheapest "real game" tell there is; snapping the number in
        // wastes the one moment the player is guaranteed to be looking at it.
        int shown = 0;
        seq.Append(DOTween.To(() => shown, v => { shown = v; finalScoreText.SetText("{0}", shown); },
                              finalScore, countDuration).SetEase(Ease.OutQuad));

        for (int i = 0; i < starCount; i++)
        {
            seq.Append(stars[i].transform
                       .DOScale(1f, 0.2f)
                       .SetEase(Ease.OutBack, overshoot: 2.5f));
            if (i < starCount - 1) seq.AppendInterval(starInterval);
        }
    }
}