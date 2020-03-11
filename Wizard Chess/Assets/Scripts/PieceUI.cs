using UnityEngine;
using System.Collections;

//Moves the UI with the corresponding piece
public class PieceUI : MonoBehaviour {
	public Transform target;
	public bool isPieceUI;
	public char color;
	// Update is called once per frame
	void Update () {
		if (isPieceUI) {
			followTarget (target.position);
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
