using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draft screen UI overlay. Shows piece grid with element selection buttons.
/// Same-scene overlay approach — no separate scene needed.
/// </summary>
public class DraftUI : MonoBehaviour
{
    public DraftManager draftManager;
    public GameObject draftPanel;

    // UI references (assigned in Inspector)
    public Text titleText;
    public Button confirmButton;
    public Button skipButton;

    // Element selection buttons
    public Button fireButton;
    public Button earthButton;
    public Button lightningButton;
    public Button noneButton;

    // Currently selected piece index for element assignment
    private int selectedPieceIndex = -1;
    private int currentColor;

    void Start()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
        if (skipButton != null)
            skipButton.onClick.AddListener(OnSkipClicked);
        if (fireButton != null)
            fireButton.onClick.AddListener(() => OnElementSelected(ChessConstants.ELEMENT_FIRE));
        if (earthButton != null)
            earthButton.onClick.AddListener(() => OnElementSelected(ChessConstants.ELEMENT_EARTH));
        if (lightningButton != null)
            lightningButton.onClick.AddListener(() => OnElementSelected(ChessConstants.ELEMENT_LIGHTNING));
        if (noneButton != null)
            noneButton.onClick.AddListener(() => OnElementSelected(ChessConstants.ELEMENT_NONE));

        if (draftPanel != null)
            draftPanel.SetActive(false);
    }

    public void ShowDraft(int playerColor)
    {
        currentColor = playerColor;
        selectedPieceIndex = -1;

        if (draftPanel != null)
            draftPanel.SetActive(true);

        if (titleText != null)
        {
            titleText.text = (playerColor == ChessConstants.WHITE ? "White" : "Black") + " - Select Elements";
        }
    }

    public void HideDraft()
    {
        if (draftPanel != null)
            draftPanel.SetActive(false);
    }

    public void SelectPiece(int pieceIndex)
    {
        selectedPieceIndex = pieceIndex;
    }

    private void OnElementSelected(int elementId)
    {
        if (selectedPieceIndex < 0 || draftManager == null) return;

        draftManager.draftData.SetElement(currentColor, selectedPieceIndex, elementId);
    }

    private void OnConfirmClicked()
    {
        if (draftManager != null)
            draftManager.ConfirmPlayerDraft();
    }

    private void OnSkipClicked()
    {
        if (draftManager != null)
            draftManager.SkipDraft();
    }
}
