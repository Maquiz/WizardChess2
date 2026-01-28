using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

//Moves the UI with the corresponding piece and updates the icon on promotion
public class PieceUI : MonoBehaviour {
	public Transform target;
	public bool isPieceUI;
	public char color;

	private Image image;
	private PieceMove pieceMove;
	private int currentPieceType = -1;

	// Shared lookup built at startup: "W_1" (White Pawn) → sprite, "B_5" (Black Queen) → sprite, etc.
	private static Dictionary<string, Sprite> spriteLookup = new Dictionary<string, Sprite>();

	void Start() {
		if (!isPieceUI) return;

		image = GetComponent<Image>();
		if (target != null)
			pieceMove = target.GetComponent<PieceMove>();

		// Register this piece's sprite in the shared lookup
		if (pieceMove != null && image != null && image.sprite != null) {
			currentPieceType = pieceMove.piece;
			string key = color + "_" + currentPieceType;
			spriteLookup[key] = image.sprite;
		}
	}

	// Update is called once per frame
	void Update () {
		if (isPieceUI) {
			followTarget (target.position);

			// Detect piece type change (e.g. pawn promotion) and swap icon
			if (pieceMove != null && pieceMove.piece != currentPieceType) {
				currentPieceType = pieceMove.piece;
				string key = color + "_" + currentPieceType;
				Sprite newSprite;
				if (spriteLookup.TryGetValue(key, out newSprite) && image != null) {
					image.sprite = newSprite;
				}
			}
		}
	}

	public void setTarget (Transform target) {
		this.target = target;
	}

	public void followTarget (Vector3 target) {
		this.transform.position = Camera.main.WorldToScreenPoint (new Vector3 (target.x, target.y, target.z));
	}

	public void setTransformPosition (Vector3 t) {
		this.transform.position = Camera.main.WorldToScreenPoint (t);
	}
}
