using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-game UI for showing and activating elemental abilities.
/// Shows an ability button when a piece with an active ability is selected,
/// displays cooldown countdown, and triggers ability mode.
/// </summary>
public class AbilityUI : MonoBehaviour
{
    public GameObject abilityPanel;
    public Button abilityButton;
    public Text abilityNameText;
    public Text cooldownText;
    public Image abilityIcon;
    public Image cooldownOverlay;

    private GameMaster gm;
    private PieceMove currentPiece;

    // Pre-loaded piece icon sprites (index 0 unused, 1-6 = PAWN..KING)
    private Sprite[] pieceSprites;

    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("GM").GetComponent<GameMaster>();

        LoadPieceSprites();

        if (abilityButton != null)
            abilityButton.onClick.AddListener(OnAbilityClicked);

        HideAbility();
    }

    private void LoadPieceSprites()
    {
        pieceSprites = new Sprite[7];
        for (int i = ChessConstants.PAWN; i <= ChessConstants.KING; i++)
        {
            pieceSprites[i] = Resources.Load<Sprite>(PieceIndexHelper.GetIconResourcePath(i));
        }
    }

    /// <summary>
    /// Ensure abilityIcon Image exists. Creates one as a child of abilityPanel if not assigned via Inspector.
    /// </summary>
    private void EnsureAbilityIcon()
    {
        if (abilityIcon != null) return;
        if (abilityPanel == null) return;

        GameObject iconObj = new GameObject("PieceIcon");
        iconObj.transform.SetParent(abilityPanel.transform, false);

        RectTransform rt = iconObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(8f, 0f);
        rt.sizeDelta = new Vector2(40f, 40f);

        abilityIcon = iconObj.AddComponent<Image>();
        abilityIcon.preserveAspect = true;
    }

    void Update()
    {
        if (gm == null) return;

        if (gm.isPieceSelected && gm.selectedPiece != null)
        {
            ShowAbilityForPiece(gm.selectedPiece);
        }
        else
        {
            HideAbility();
        }
    }

    public void ShowAbilityForPiece(PieceMove piece)
    {
        if (piece == null || piece.elementalPiece == null || piece.elementalPiece.active == null)
        {
            HideAbility();
            return;
        }

        currentPiece = piece;
        ElementalPiece ep = piece.elementalPiece;

        if (abilityPanel != null)
            abilityPanel.SetActive(true);

        // Update cooldown display
        if (cooldownText != null)
        {
            if (ep.cooldown.IsReady)
            {
                cooldownText.text = "READY";
                cooldownText.color = Color.green;
            }
            else
            {
                cooldownText.text = ep.cooldown.CurrentCooldown.ToString();
                cooldownText.color = Color.red;
            }
        }

        // Update button interactability
        if (abilityButton != null)
        {
            abilityButton.interactable = ep.cooldown.IsReady &&
                ep.active.CanActivate(piece, gm.boardState, gm.squareEffectManager);
        }

        // Update cooldown overlay
        if (cooldownOverlay != null && ep.cooldown.MaxCooldown > 0)
        {
            cooldownOverlay.fillAmount = (float)ep.cooldown.CurrentCooldown / ep.cooldown.MaxCooldown;
        }

        // Update name
        if (abilityNameText != null)
        {
            abilityNameText.text = GetAbilityName(ep.elementId, piece.piece);
        }

        // Update piece icon
        EnsureAbilityIcon();
        if (abilityIcon != null && pieceSprites != null)
        {
            int pt = piece.piece;
            if (pt >= ChessConstants.PAWN && pt <= ChessConstants.KING)
            {
                abilityIcon.sprite = pieceSprites[pt];
                abilityIcon.color = GetElementColor(ep.elementId);
            }
        }
    }

    public void HideAbility()
    {
        currentPiece = null;
        if (abilityPanel != null)
            abilityPanel.SetActive(false);
    }

    private void OnAbilityClicked()
    {
        if (currentPiece != null && gm != null)
        {
            gm.EnterAbilityMode(currentPiece);
        }
    }

    private string GetAbilityName(int elementId, int pieceType)
    {
        if (elementId == ChessConstants.ELEMENT_FIRE)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Flame Rush";
                case ChessConstants.ROOK: return "Inferno Line";
                case ChessConstants.KNIGHT: return "Eruption";
                case ChessConstants.BISHOP: return "Flame Cross";
                case ChessConstants.QUEEN: return "Meteor Strike";
                case ChessConstants.KING: return "Backdraft";
            }
        }
        else if (elementId == ChessConstants.ELEMENT_EARTH)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Barricade";
                case ChessConstants.ROOK: return "Rampart";
                case ChessConstants.KNIGHT: return "Earthquake";
                case ChessConstants.BISHOP: return "Petrify";
                case ChessConstants.QUEEN: return "Continental Divide";
                case ChessConstants.KING: return "Sanctuary";
            }
        }
        else if (elementId == ChessConstants.ELEMENT_LIGHTNING)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Chain Strike";
                case ChessConstants.ROOK: return "Thunder Strike";
                case ChessConstants.KNIGHT: return "Lightning Rod";
                case ChessConstants.BISHOP: return "Arc Flash";
                case ChessConstants.QUEEN: return "Tempest";
                case ChessConstants.KING: return "Static Field";
            }
        }
        return "Ability";
    }

    private Color GetElementColor(int elementId)
    {
        switch (elementId)
        {
            case ChessConstants.ELEMENT_FIRE: return new Color(1f, 0.5f, 0.2f);
            case ChessConstants.ELEMENT_EARTH: return new Color(0.8f, 0.7f, 0.3f);
            case ChessConstants.ELEMENT_LIGHTNING: return new Color(0.5f, 0.8f, 1f);
            default: return Color.white;
        }
    }
}
