using UnityEngine;
using System.Collections;

public class RockGiantDamage : MonoBehaviour {

	public float rockGiantDamage = 6f;
	PlayerController ownPlayerController;

	void Start () {
		ownPlayerController = gameObject.GetComponentInParent<PlayerController>();
	}

	void OnTriggerEnter(Collider other) {
		//Enemy or own player collision?
		PlayerController otherPlayerController = other.gameObject.GetComponentInParent<PlayerController>();
		if (otherPlayerController != null && ownPlayerController != null && ownPlayerController != otherPlayerController) 
		{
			otherPlayerController.damage(rockGiantDamage);
		}
	}
}
