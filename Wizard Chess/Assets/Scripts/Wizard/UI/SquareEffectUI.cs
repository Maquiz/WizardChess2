using UnityEngine;

/// <summary>
/// Visual indicators for square effects. Tints the square material based on the
/// active effect type (fire = red/orange, stone wall = grey/brown, lightning = yellow/blue).
/// Attach to each Square GameObject or manage globally.
/// </summary>
public class SquareEffectUI : MonoBehaviour
{
    private Square square;
    private Renderer squareRenderer;
    private Color originalColor;
    private bool hasOriginalColor = false;

    void Start()
    {
        square = GetComponent<Square>();
        squareRenderer = GetComponent<Renderer>();
        if (squareRenderer != null)
        {
            originalColor = squareRenderer.material.color;
            hasOriginalColor = true;
        }
    }

    void Update()
    {
        if (square == null || squareRenderer == null) return;

        if (square.activeEffect != null)
        {
            Color effectColor = GetEffectColor(square.activeEffect.effectType);
            squareRenderer.material.color = Color.Lerp(originalColor, effectColor, 0.6f);
        }
        else if (hasOriginalColor)
        {
            squareRenderer.material.color = originalColor;
        }
    }

    private Color GetEffectColor(SquareEffectType type)
    {
        switch (type)
        {
            case SquareEffectType.Fire:
                return new Color(1f, 0.3f, 0f, 1f); // Orange-red
            case SquareEffectType.StoneWall:
                return new Color(0.5f, 0.4f, 0.3f, 1f); // Brown-grey
            case SquareEffectType.LightningField:
                return new Color(0.3f, 0.6f, 1f, 1f); // Electric blue
            default:
                return originalColor;
        }
    }
}
