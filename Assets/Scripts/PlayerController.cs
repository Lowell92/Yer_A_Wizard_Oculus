using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public float life_starting = 10f;
	public float shieldingDamageFactor = 0.05f;
	public float giantMaxGrowFactor = 10f;
	private float life;
	private bool playerAlive = true;
	private bool isShielded = false;
	private bool isGiant = false;
	private float shieldTimeleft = 0f;
	private float giantTimeleft = 0f;
	private float giantGrowFactor = 1f;
	private float damageTakenBlinkTimer = 0f;
	private MeshRenderer[] ownMeshRenderers;
	private RockGiantDamage[] rockGiantDamageScripts;



	void Awake () {
		life = life_starting;
		ownMeshRenderers = GetComponentsInChildren<MeshRenderer>();
		rockGiantDamageScripts = GetComponentsInChildren<RockGiantDamage>();
		//Remove Sixense Control if not own player
		if (!GetComponent<NetworkView> ().isMine) {
			GetComponentInChildren<SixenseHandsController> ().enabled = false;
			foreach (SixenseHand sh in GetComponentsInChildren<SixenseHand> ()) sh.enabled = false;
			Destroy(gameObject.GetComponentInChildren<Camera>().gameObject);
		}
		foreach(RockGiantDamage rgd in rockGiantDamageScripts) rgd.enabled = false;
	}

	void Start () {

	}

	void Update () {
		if (isShielded) WhileBeingShielded();
		if (isGiant) WhileBeingGiant();
		//Damage indicator (red blinking)
		if (damageTakenBlinkTimer > 0) damageTakenBlink();
	}

	public void damage(float damage) {
		if (isShielded) damage *= shieldingDamageFactor;
		if (damage > 0f) 
		{
			setLife(getLife()-damage);
			damageTakenBlinkTimer = 3+damage;
		}
		//TODO damage effect
	}

	public void heal(float healAmount) {
		if (healAmount > 0f) setLife(getLife()+healAmount);
		//TODO heal effect
	}

	public void killPlayer(){
		life = 0f;
		playerAlive = false;
	}

	public void setLife(float life) {
		this.life = Mathf.Clamp(life,0f,10000f);
		if (this.life == 0f) killPlayer();
		//TODO send status information to other player/network instance
	}

	public float getLife() {
		return life;
	}

	public bool getIsShielded() {
		return isShielded;
	}

	public bool getIsGiant() {
		return isGiant;
	}

	public void shieldPlayer(float timeleft, bool addTime = true) {
		if (playerAlive && timeleft > 0)
		{
			if (addTime) shieldTimeleft += timeleft; else shieldTimeleft = timeleft;
			isShielded = true;
		}
		//TODO send status information to other player/network instance
		//TODO effect
	}

	public void unshieldPlayer() {
		shieldTimeleft = 0;
		isShielded = false;
		//TODO send status information to other player/network instance
	}

	private void WhileBeingShielded() {
		//Timeleft
		shieldTimeleft -= Time.deltaTime;
		if (shieldTimeleft <= 0f) unshieldPlayer();
	}

	public void giantPlayer(float timeleft, bool addTime = true) {
		if (playerAlive && timeleft > 0)
		{
			if (addTime) giantTimeleft += timeleft; else giantTimeleft = timeleft;
			isGiant = true;
			giantGrowFactor = 1f;
		}
		//TODO send status information to other player/network instance
		//TODO effect
	}

	public void ungiantPlayer() {
		giantTimeleft = 0;
		isGiant = false;
		giantGrowFactor = 1f;
		transform.localScale = new Vector3(giantGrowFactor,giantGrowFactor,giantGrowFactor);
		transform.localPosition = new Vector3(transform.localPosition.x,(1f-transform.localScale.y)*2f/3f,transform.localPosition.z);
		//TODO send status information to other player/network instance
	}

	private void WhileBeingGiant() {
		//Growing effect
		if (giantGrowFactor < giantMaxGrowFactor && giantTimeleft > 2f)
		{
			giantGrowFactor += (giantMaxGrowFactor/2f)*Time.deltaTime;
			transform.localScale = new Vector3(giantGrowFactor,giantGrowFactor,giantGrowFactor);
			transform.localPosition = new Vector3(transform.localPosition.x,(1f-transform.localScale.y)*2f/3f,transform.localPosition.z);
		}
		//Shrinking effect
		if (giantGrowFactor > 1 && giantTimeleft <= 2f)
		{
			giantGrowFactor -= ((giantMaxGrowFactor-1f)/2f)*Time.deltaTime;
			if (giantGrowFactor < 1f) giantGrowFactor = 1f;
			transform.localScale = new Vector3(giantGrowFactor,giantGrowFactor,giantGrowFactor);
			transform.localPosition = new Vector3(transform.localPosition.x,(1f-transform.localScale.y)*2f/3f,transform.localPosition.z);
			
		}
		//Damage only if a certain size is reached
		if (giantGrowFactor > giantMaxGrowFactor/2f)
		{
			foreach(RockGiantDamage rgd in rockGiantDamageScripts) rgd.enabled = true;
		} else {
			foreach(RockGiantDamage rgd in rockGiantDamageScripts) rgd.enabled = false;
		}
		//Timeleft
		giantTimeleft -= Time.deltaTime;
		if (giantTimeleft <= 0f) ungiantPlayer();
	}

	private void damageTakenBlink() {
		if (damageTakenBlinkTimer > 0f) 
		{
			damageTakenBlinkTimer -= 10f*Time.deltaTime;
			if (damageTakenBlinkTimer <= 0f) 
				foreach(MeshRenderer mr in ownMeshRenderers) mr.material.color = Color.white;
			else
				foreach(MeshRenderer mr in ownMeshRenderers) mr.material.color = new Color((int)damageTakenBlinkTimer % 2, ((int)damageTakenBlinkTimer+1) % 2, ((int)damageTakenBlinkTimer+1) % 2);
		}
	}

}
