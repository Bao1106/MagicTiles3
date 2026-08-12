using UnityEngine;
using UnityEngine.UI.ProceduralImage;

/// <summary>
/// Draws the vertical lane separators.
/// Bars are anchored at viewport fraction i/laneCount — the exact boundaries
/// InputController derives a lane from — so the lines cannot drift away from where a lane
/// actually starts on any aspect ratio, without a single line of per-frame layout code.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class LaneDividerView : MonoBehaviour
{
    [Tooltip("Source of truth for lane count. Falls back to laneCountFallback when unset.")]
    [SerializeField] private NoteController noteController;
    [SerializeField, Range(2, 6)] private int laneCountFallback = 4;

    [Header("Bar")]
    [Tooltip("Width in canvas reference units (CanvasScaler reference width is 750).")]
    [SerializeField] private float width = 4f;
    [SerializeField] private float cornerRadius = 2f;
    [SerializeField] private Color color = new Color(1f, 1f, 1f, 0.18f);

    [Tooltip("Also draw a bar on the two outer screen edges, not just between lanes.")]
    [SerializeField] private bool includeEdges;

    private void Start() => Rebuild();

    public void Rebuild()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        int lanes = noteController != null ? noteController.LaneCount : laneCountFallback;

        int first = includeEdges ? 0 : 1;
        int last = includeEdges ? lanes : lanes - 1;

        for (int i = first; i <= last; i++)
            CreateBar((float)i / lanes);
    }

    private void CreateBar(float viewportX)
    {
        var rect = new GameObject("LaneDivider", typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(transform, false);

        // Zero-width anchor span pinned to the boundary, stretched over full height.
        rect.anchorMin = new Vector2(viewportX, 0f);
        rect.anchorMax = new Vector2(viewportX, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, 0f);

        // Modifier goes on first: ProceduralImage auto-adds a FreeModifier the moment its own
        // Modifier getter runs, and [DisallowMultipleComponent] would then reject ours.
        UniformModifier modifier = rect.gameObject.AddComponent<UniformModifier>();
        ProceduralImage image = rect.gameObject.AddComponent<ProceduralImage>();

        image.color = color;
        image.raycastTarget = false;   // decorative — must never intercept a lane tap
        modifier.Radius = cornerRadius;
    }
}
