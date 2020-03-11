using UnityEngine;
using System.Collections;

public class OutofBounds : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Out Of Bounds");
        PieceMove p = other.GetComponent<PieceMove>();
        p.returnpiece();

    }
    // Update is called once per frame
    void Update () {
	
	}
}
