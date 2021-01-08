using UnityEngine;
using System.Collections;
using DG.Tweening;

public class CameraMove : MonoBehaviour {
    public Transform C; //camera
	// Use this for initialization
	void Start () {
        // Initialize DOTween (needs to be done only once).
        // If you don't initialize DOTween yourself,
        // it will be automatically initialized with default values.
        DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
      //  TopMove();
    }
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            player1Move();
        } else if (Input.GetKeyDown(KeyCode.Alpha2)) {
            player2Move();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TopMove();
        }
    }
    void player1Move() {
        C.DOMove(new Vector3(8 , 12, 3.62f), 1);
        C.transform.localEulerAngles = new Vector3(90, 180, 0);
            }
    void player2Move() {
        C.DOMove(new Vector3(-1f, 12, 3.62f), 1);
        C.transform.localEulerAngles = new Vector3(90, 0, 0);
    }
    void TopMove() {
        C.DOMove(new Vector3(-7, 12, 7), 1);
        C.transform.localEulerAngles = new Vector3(90, 0, 0);
    }

}
