﻿using UnityEngine;
using System.Collections;

//The Square where only one piece can reside
public class Square : MonoBehaviour {
	private AudioSource hit;
	//Sound when piece hits the square
	public bool taken = false;
	public GameObject piece;
	public int x, y;

	//This should be a ascii calc so it can have a length of N char 0 - A ...
	public string[] letters = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K" };

	void Start () {
		//Find Hit Sound
		hit = GameObject.Find ("HitBoard").GetComponent<AudioSource> () as AudioSource;
	}

	void OnCollisionEnter (Collision collision) {
		//Play Hit Sound
		hit.Play ();
		taken = true;
		piece = collision.gameObject;
	}

	void OnTriggerEnter (Collider other) {
		PieceMove p = other.GetComponent<PieceMove> ();
		if (p.getIsSet () == false) {
			p.movePiece (x, y, this);
		} else {
			p.setIntitialPiece (x, y, this.gameObject);
		}

		/*
		if (s.taken) {
			s.piece.transform.DOJump (new Vector3 (-100.0f, -100.0f),1000,1,0.0f,false);
			Transform t = other.transform;
			t.DOPause ();
			t.DOMove (new Vector3 (this.transform.position.x, this.transform.position.y, this.transform.position.z), .3f);
			Debug.Log(p.color.ToString() + p.piece.ToString() + " to " + x.ToString() + y);
			p.last = s.gameObject;
			s.piece = other.gameObject;
			p.checkMove (x,y);
		} else {

		}*/
	}
}