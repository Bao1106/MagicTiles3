using UnityEngine;

[CreateAssetMenu(fileName = "JudgementPalette", menuName = "MagicTile/Judgement Palette")]
public class JudgementPalette : ScriptableObject
{
    [Header("Colours")]
    [ColorUsage(true, true)] [SerializeField] private Color perfect = new Color(1f, 0.847f, 0.302f);
    [ColorUsage(true, true)] [SerializeField] private Color good = new Color(0.133f, 0.894f, 1f);
    [ColorUsage(true, true)] [SerializeField] private Color miss = new Color(1f, 0.278f, 0.341f);

    [Header("Labels")]
    [SerializeField] private string perfectLabel = "PERFECT";
    [SerializeField] private string goodLabel = "GOOD";
    [SerializeField] private string missLabel = "MISS";

    public Color ColorOf(Judgement grade) => grade switch
    {
        Judgement.Perfect => perfect,
        Judgement.Good => good,
        _ => miss
    };

    public string LabelOf(Judgement grade) => grade switch
    {
        Judgement.Perfect => perfectLabel,
        Judgement.Good => goodLabel,
        _ => missLabel
    };
}