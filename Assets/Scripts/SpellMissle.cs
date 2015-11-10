using UnityEngine;
using System.Collections;

public class SpellMissle : MonoBehaviour {

	public float speed = 5f;

	// Use this for initialization
	void Start () {
		Destroy(gameObject,2f);
	}
	
	// Update is called once per frame
	void Update () {
		Debug.DrawLine (transform.position, transform.position + transform.forward * 2f, Color.red);
		transform.position = transform.position + transform.forward * Time.deltaTime * speed;
//		transform.Translate(transform.forward*Time.deltaTime*speed); //bugged??
	}
}
