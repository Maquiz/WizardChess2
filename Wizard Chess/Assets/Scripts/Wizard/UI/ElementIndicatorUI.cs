using UnityEngine;

/// <summary>
/// Displays an element indicator on pieces that have an element assigned.
/// Tints the piece material slightly based on their element color.
/// </summary>
public class ElementIndicatorUI : MonoBehaviour
{
    private PieceMove pieceMove;
    private Renderer pieceRenderer;
    private Color originalColor;
    private bool initialized = false;

    void Start()
    {
        pieceMove = GetComponent<PieceMove>();
        pieceRenderer = GetComponent<Renderer>();

        if (pieceRenderer != null)
        {
            originalColor = pieceRenderer.material.color;
        }
    }

    void LateUpdate()
    {
        if (pieceMove == null || pieceRenderer == null) return;

        if (!initialized && pieceMove.elementalPiece != null)
        {
            Color tint = GetElementColor(pieceMove.elementalPiece.elementId);
            pieceRenderer.material.color = Color.Lerp(originalColor, tint, 0.25f);
            initialized = true;
        }
    }

    private Color GetElementColor(int elementId)
    {
        switch (elementId)
        {
            case ChessConstants.ELEMENT_FIRE:
                return new Color(1f, 0.4f, 0.1f); // Orange
            case ChessConstants.ELEMENT_EARTH:
                return new Color(0.6f, 0.5f, 0.2f); // Brown-gold
            case ChessConstants.ELEMENT_LIGHTNING:
                return new Color(0.4f, 0.7f, 1f); // Light blue
            default:
                return originalColor;
        }
    }
}
