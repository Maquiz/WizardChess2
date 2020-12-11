using UnityEngine;
using System.Collections;

public class OutofBounds : MonoBehaviour {
    void OnTriggerEnter(Collider other) {
        Debug.Log("Out Of Bounds");
        PieceMove p = other.GetComponent<PieceMove>();
        p.returnpiece();
    }
}
