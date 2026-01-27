using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mouse-over tooltip that shows piece name, element, and ability info.
/// Attach to the GameMaster GameObject. Creates its own UI elements at runtime.
/// </summary>
public class PieceTooltipUI : MonoBehaviour
{
    private GameMaster gm;
    private GameObject tooltipPanel;
    private Text tooltipText;
    private Canvas canvas;
    private PieceMove lastHoveredPiece;
    private RectTransform tooltipRect;

    void Start()
    {
        gm = GetComponent<GameMaster>();
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            CreateTooltipUI();
        }
    }

    private void CreateTooltipUI()
    {
        // Create tooltip panel
        tooltipPanel = new GameObject("PieceTooltip");
        tooltipPanel.transform.SetParent(canvas.transform, false);

        tooltipRect = tooltipPanel.AddComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(300, 200);
        tooltipRect.pivot = new Vector2(0, 1);

        // Background
        Image bg = tooltipPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

        // Add outline via a second image as border
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(tooltipPanel.transform, false);
        RectTransform borderRt = borderObj.AddComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = new Vector2(-1, -1);
        borderRt.offsetMax = new Vector2(1, 1);
        borderRt.SetAsFirstSibling();
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = new Color(0.4f, 0.4f, 0.5f, 0.8f);

        // Text content
        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(tooltipPanel.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10, 8);
        textRt.offsetMax = new Vector2(-10, -8);

        tooltipText = textObj.AddComponent<Text>();
        tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tooltipText.fontSize = 13;
        tooltipText.color = Color.white;
        tooltipText.alignment = TextAnchor.UpperLeft;
        tooltipText.supportRichText = true;
        tooltipText.verticalOverflow = VerticalWrapMode.Overflow;
        tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;

        tooltipPanel.SetActive(false);
    }

    void Update()
    {
        if (canvas == null || tooltipPanel == null) return;

        // Don't show tooltip during ability mode or draft phase
        if (gm != null && gm.isDraftPhase) { HideTooltip(); return; }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject.CompareTag("Piece"))
            {
                PieceMove pm = hit.collider.gameObject.GetComponent<PieceMove>();
                if (pm != null)
                {
                    ShowTooltip(pm);
                    PositionTooltip();
                    return;
                }
            }
        }

        HideTooltip();
    }

    private void ShowTooltip(PieceMove pm)
    {
        if (lastHoveredPiece == pm && tooltipPanel.activeSelf) return;
        lastHoveredPiece = pm;

        string text = BuildTooltipText(pm);
        tooltipText.text = text;

        // Auto-size height based on content
        float preferredHeight = tooltipText.preferredHeight + 20;
        tooltipRect.sizeDelta = new Vector2(300, Mathf.Max(preferredHeight, 60));

        tooltipPanel.SetActive(true);
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            tooltipPanel.SetActive(false);
            lastHoveredPiece = null;
        }
    }

    private string BuildTooltipText(PieceMove pm)
    {
        string pieceName = pm.printPieceName();
        string position = pm.printSquare(pm.curx, pm.cury);

        string text = "<b>" + pieceName + "</b>  (" + position + ")\n";

        if (pm.elementalPiece != null)
        {
            ElementalPiece ep = pm.elementalPiece;
            string elemName = AbilityInfo.GetElementName(ep.elementId);
            Color elemColor = GetElementTextColor(ep.elementId);
            string hexColor = ColorUtility.ToHtmlStringRGB(elemColor);
            text += "<color=#" + hexColor + ">Element: " + elemName + "</color>\n\n";

            // Passive
            string passiveName = AbilityInfo.GetPassiveName(ep.elementId, pm.piece);
            string passiveDesc = AbilityInfo.GetPassiveDescription(ep.elementId, pm.piece);
            text += "<b>Passive:</b> " + passiveName + "\n";
            text += "<color=#CCCCCC>" + passiveDesc + "</color>\n\n";

            // Active
            string activeName = AbilityInfo.GetActiveName(ep.elementId, pm.piece);
            string activeDesc = AbilityInfo.GetActiveDescription(ep.elementId, pm.piece);
            string cdInfo = "";
            if (ep.cooldown != null)
            {
                if (ep.cooldown.IsReady)
                    cdInfo = "<color=#44FF44>READY</color>";
                else
                    cdInfo = "<color=#FF4444>" + ep.cooldown.CurrentCooldown + " turns</color>";
            }
            text += "<b>Active [Q]:</b> " + activeName + "  " + cdInfo + "\n";
            text += "<color=#CCCCCC>" + activeDesc + "</color>";

            // Status effects with descriptions
            if (ep.IsStunned())
            {
                text += "\n\n<color=#" + AbilityInfo.GetStatusEffectColor(StatusEffectType.Stunned) + "><b>"
                    + AbilityInfo.GetStatusEffectName(StatusEffectType.Stunned) + "</b></color>";
                text += "\n<color=#AAAAAA>" + AbilityInfo.GetStatusEffectDescription(StatusEffectType.Stunned) + "</color>";
            }
            if (ep.IsSinged())
            {
                text += "\n\n<color=#" + AbilityInfo.GetStatusEffectColor(StatusEffectType.Singed) + "><b>"
                    + AbilityInfo.GetStatusEffectName(StatusEffectType.Singed) + "</b></color>";
                text += "\n<color=#AAAAAA>" + AbilityInfo.GetStatusEffectDescription(StatusEffectType.Singed) + "</color>";
            }
        }

        // Square effect on current square
        if (pm.curSquare != null && pm.curSquare.activeEffect != null)
        {
            SquareEffectType seType = pm.curSquare.activeEffect.effectType;
            if (seType != SquareEffectType.None)
            {
                string seColor = AbilityInfo.GetSquareEffectColor(seType);
                text += "\n\n<color=#" + seColor + "><b>Standing on: "
                    + AbilityInfo.GetSquareEffectName(seType) + "</b></color>";
                text += "\n<color=#AAAAAA>" + AbilityInfo.GetSquareEffectDescription(seType) + "</color>";
            }
        }

        return text;
    }

    private Color GetElementTextColor(int elementId)
    {
        switch (elementId)
        {
            case ChessConstants.ELEMENT_FIRE: return new Color(1f, 0.5f, 0.2f);
            case ChessConstants.ELEMENT_EARTH: return new Color(0.8f, 0.7f, 0.3f);
            case ChessConstants.ELEMENT_LIGHTNING: return new Color(0.5f, 0.8f, 1f);
            default: return Color.white;
        }
    }

    private void PositionTooltip()
    {
        Vector2 pos = Input.mousePosition;
        pos.x += 20;
        pos.y -= 10;

        // Keep on screen
        if (pos.x + tooltipRect.sizeDelta.x > Screen.width)
            pos.x = Screen.width - tooltipRect.sizeDelta.x - 5;
        if (pos.y - tooltipRect.sizeDelta.y < 0)
            pos.y = tooltipRect.sizeDelta.y + 5;

        tooltipRect.position = pos;
    }
}
