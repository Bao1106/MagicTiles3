using UnityEngine;

public class TileView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;

    /// <summary>Index into the controller's NoteData array. Set on spawn.</summary>
    public int NoteIndex { get; set; }

    public void SetSize(float width, float height)
    {
        // The greybox sprite is a 1x1 unit square, so localScale maps straight to world size.
        transform.localScale = new Vector3(width, height, 1f);
    }

    public void SetPosition(float x, float y)
    {
        transform.position = new Vector3(x, y, 0f);
    }

    public void SetColor(Color color)
    {
        body.color = color;
    }
}