using UnityEngine;
using System.Collections;

//Checks if a piece is on top of another piece
public class PieceCheck : MonoBehaviour {
    void OnTriggerEnter(Collider other) {
        Debug.Log("On Top");
        PieceMove p = other.GetComponent<PieceMove>();
        p.returnpiece();

    }
}