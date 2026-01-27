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

    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("GM").GetComponent<GameMaster>();

        if (abilityButton != null)
            abilityButton.onClick.AddListener(OnAbilityClicked);

        HideAbility();
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
}
