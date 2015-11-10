using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ActivateSpell : MonoBehaviour {

	Transform spellCastPatricleTransformLeft, spellCastPatricleTransformRight;
	public Transform spellMisslePrefab;

	PlayerController localPlayerController;
	public Transform leftHand, rightHand;
	Transform camTransform;

	//Spells
	readonly int _spellTotalCount = 4;
	private Vector3[] offsetsLeft_fireball,offsetsRight_fireball, offsetsLeft_shield, offsetsRight_shield, offsetsLeft_teleport, offsetsRight_teleport;
	private Vector3[,] offsetsSpell_left, offsetsSpell_right;
	private int[] offsetsSpellCount_left,offsetsSpellCount_right;
	private string[] spellMethodName;
	private float[] spellGestureSizeMultiplier;

	//Casting
	readonly float _newPointMinimumDistance = 0.02f; //Offset, at which a new controller position is added to the gesture point list. Smaller value = higher resolution, but also higher change to randomly cast a spell.
	readonly float _goBackDistanceForAverageAngleVector = 0.2f; //Maximum distance over which the average gesture angle will be calculated
	readonly float _spellCastAngleTolerance = 0.9f; //How exact does the caster have to match all the angles? Higher value = more strict. 1.0f equals 100% match (dot product)
	readonly int _maximumGesturePointsStored = 50; //Maximum length of gesture point list. Should be big enough so it can store (_goBackDistanceForAverageAngleVector/_newPointMinimumDistance) vectors.
	readonly float _spellGeneralCooldown = 1f; //Cooldown after successful cast
	readonly float _minimumGestureLength = 0.2f; 
		/*
		 * Note that _minimumGestureLength correlates with _newPointMinimumDistance! Because with a low value of _newPointMinimumDistance, 
		 * all angles can easily be reached within a very small range (below _minimumGestureLength). 
		 * On the upside: Straight line gestures have to be very quick, so _newPointMinimumDistance will be exceeded by every point.
		 * (Downside: FPS dependent - but high FPS should always be reached anyways due to HMD.)
		*/
	private int[] currentSpellOffsetToApprove; //Count for every spell extra. Otherwise you could activate 90% of spell x and then cast spell y by hitting its last vector.
	private float[] gestureLength; //Calc for every spell extra. Same reason as above.
	private Vector3[] previousApprovalPosition;
	private List<Vector3> pointsLeftHand, pointsRightHand;
	private float cooldown = 0f;

	//Debug
	public int debugTrackSpell = 1;



	void Awake () {
		Application.targetFrameRate = 75;

		localPlayerController = transform.parent.GetComponent<PlayerController>();
		camTransform = GameObject.FindObjectOfType<Camera>().transform;
		spellCastPatricleTransformLeft = GameObject.Find("SpellCoordinateLeft").transform;
		spellCastPatricleTransformRight = GameObject.Find("SpellCoordinateRight").transform;
		if (leftHand == null) leftHand = GameObject.Find("Hand - Left").transform;
		if (rightHand == null) rightHand = GameObject.Find("Hand - Right").transform;
		pointsLeftHand = new List<Vector3>();
		pointsRightHand = new List<Vector3>();
		currentSpellOffsetToApprove = new int[_spellTotalCount];
		gestureLength = new float[_spellTotalCount];
		previousApprovalPosition = new Vector3[_spellTotalCount];

		initializeSpells();
		abortSpellCast();
	}

	void abortSpellCast() {
		abortSpellCast (-1);
	}

	void abortSpellCast(int spellNumber) {
		spellCastPatricleTransformLeft.gameObject.SetActive(false);
		spellCastPatricleTransformRight.gameObject.SetActive(false);
		if (spellNumber == -1) {
			for (int k = 0; k < _spellTotalCount; k++) {
				currentSpellOffsetToApprove [k] = 0;
				gestureLength [k] = 0f;
			}
		} else {
			currentSpellOffsetToApprove [spellNumber] = 0;
			gestureLength [spellNumber] = 0f;
		}
	}
	

	void Update () {
		if (cooldown > 0.0001f) cooldown -= Time.deltaTime;
		Vector3 leftHandProjected = leftHand.position; //Keine Projektion notwendig
		Vector3 rightHandProjected = rightHand.position;
		//Only add points when there is a minimum offset (otherwise too many points @high frame rate)
		if (pointsLeftHand.Count == 0 || Vector3.Distance(leftHandProjected,pointsLeftHand[pointsLeftHand.Count-1]) > _newPointMinimumDistance) pointsLeftHand.Add(leftHandProjected);
		if (pointsRightHand.Count == 0 || Vector3.Distance(rightHandProjected,pointsRightHand[pointsRightHand.Count-1]) > _newPointMinimumDistance) pointsRightHand.Add(rightHandProjected);
		if (pointsLeftHand.Count > _maximumGesturePointsStored) pointsLeftHand.RemoveAt(0);
		if (pointsRightHand.Count > _maximumGesturePointsStored) pointsRightHand.RemoveAt(0);
		if (cooldown <= 0f && pointsLeftHand.Count > 1 && pointsRightHand.Count > 1)
		{
			//Calculate average angle of motion, taking into accounts points behind the current position until a distance of _goBackDistanceForAverageAngleVector
			Vector3 averageOffsetLeftHand = Vector3.zero;
			int i = pointsLeftHand.Count-1;
			do {
				if (Vector3.Distance(pointsLeftHand[i-1],leftHandProjected) < _goBackDistanceForAverageAngleVector) 
					averageOffsetLeftHand += (pointsLeftHand[i] - pointsLeftHand[i-1]).normalized;
					else break;
				i--;
			} while (i > 0);
			averageOffsetLeftHand /= (float)(pointsLeftHand.Count-1-i);
			Vector3 averageOffsetRightHand = Vector3.zero;
			i = pointsRightHand.Count-1;
			do {
				if (Vector3.Distance(pointsRightHand[i-1],rightHandProjected) < _goBackDistanceForAverageAngleVector) 
					averageOffsetRightHand += (pointsRightHand[i] - pointsRightHand[i-1]).normalized;
					else break;
				i--;
			} while (i > 0);
			averageOffsetRightHand /= (float)(pointsRightHand.Count-1-i);
			//Debug
			Debug.DrawLine(leftHand.position+leftHandProjected,leftHand.position+leftHandProjected+averageOffsetLeftHand,Color.magenta);
			Debug.DrawLine(leftHand.position+leftHandProjected,leftHand.position+leftHandProjected+offsetsSpell_left[debugTrackSpell,currentSpellOffsetToApprove[debugTrackSpell]],new Color(
				Mathf.Max(0f,Vector3.Dot(averageOffsetLeftHand,offsetsSpell_left[debugTrackSpell,currentSpellOffsetToApprove[debugTrackSpell]])),
				Mathf.Max(0f,-Vector3.Dot(averageOffsetLeftHand,offsetsSpell_left[debugTrackSpell,currentSpellOffsetToApprove[debugTrackSpell]])),
				0));
			Debug.DrawLine(rightHand.position+rightHandProjected,rightHand.position+rightHandProjected+averageOffsetRightHand,Color.magenta);
			Debug.DrawLine(rightHand.position+rightHandProjected,rightHand.position+rightHandProjected+offsetsSpell_right[debugTrackSpell,currentSpellOffsetToApprove[debugTrackSpell]],new Color(
				Mathf.Max(0f,Vector3.Dot(averageOffsetRightHand,offsetsSpell_right[debugTrackSpell,currentSpellOffsetToApprove[debugTrackSpell]])),
				Mathf.Max(0f,-Vector3.Dot(averageOffsetRightHand,offsetsSpell_right[debugTrackSpell,currentSpellOffsetToApprove[debugTrackSpell]])),
				0));
			//Scalar product, check all spells
			for (int s = 0; s < _spellTotalCount; s++)
			{
				bool leftHandCorrect = Vector3.Dot(averageOffsetLeftHand,offsetsSpell_left[s,currentSpellOffsetToApprove[s]]) > _spellCastAngleTolerance;
				bool rightHandCorrect = Vector3.Dot(averageOffsetRightHand,offsetsSpell_right[s,currentSpellOffsetToApprove[s]]) > _spellCastAngleTolerance;
				if (leftHandCorrect && rightHandCorrect)
				{
					//Casting effect
					spellCastPatricleTransformLeft.gameObject.SetActive(true);
					spellCastPatricleTransformRight.gameObject.SetActive(true);
					spellCastPatricleTransformLeft.position = leftHand.position;
					spellCastPatricleTransformRight.position = rightHand.position;
					//Cancel cast after 1 second with no offset match
					CancelInvoke("abortSpellCast");
					Invoke("abortSpellCast",0.5f);
					//Progress
					currentSpellOffsetToApprove[s]++;
					//Calculate gesture length
					if (currentSpellOffsetToApprove[s] > 1) 
					{
						//Use the controller (side) which has to be moved more
						if (offsetsSpellCount_left[s] > offsetsSpellCount_right[s]) 
						{
							gestureLength[s] += Vector3.Distance(previousApprovalPosition[s],leftHandProjected);
						} else {
							gestureLength[s] += Vector3.Distance(previousApprovalPosition[s],rightHandProjected);
						}
					}
					if (offsetsSpellCount_left[s] > offsetsSpellCount_right[s])
						previousApprovalPosition[s] = leftHandProjected;
					else
						previousApprovalPosition[s] = rightHandProjected;
					//Cast successful?
					if (currentSpellOffsetToApprove[s] >= offsetsSpellCount_left[s] && currentSpellOffsetToApprove[s] >= offsetsSpellCount_right[s]) 
					{
						print(gestureLength[s] + "  /  " + (_minimumGestureLength * spellGestureSizeMultiplier[s]));
						if (gestureLength[s] >= _minimumGestureLength * spellGestureSizeMultiplier[s])
						{
							cooldown = _spellGeneralCooldown;
							Invoke(spellMethodName[s],0f); print(s);
							abortSpellCast();
							CancelInvoke("abortSpellCast");
						} else {print("too short gesture!");
							abortSpellCast(s);
						}
					}
				}
			}
		}
	}


	//Spell definitions

	void initializeSpells() {
		offsetsSpell_left = new Vector3[_spellTotalCount,6];
		offsetsSpell_right = new Vector3[_spellTotalCount,6];
		offsetsSpellCount_left = new int[_spellTotalCount];
		offsetsSpellCount_right = new int[_spellTotalCount];
		spellMethodName = new string[_spellTotalCount];
		spellGestureSizeMultiplier = new float[_spellTotalCount];
		//There should be at least 4-5 offsets for every spell so it isn't cast all at random

		//Fireball - Vorwärtsbewegung
		int s = 0;
		spellMethodName[s] = "cast_fireball";
		spellGestureSizeMultiplier[s] = 0.5f;
		offsetsSpellCount_left[s] = 6;
		offsetsSpellCount_right[s] = 6;
		for (int i = 0; i < offsetsSpellCount_left[s]; i++) offsetsSpell_left[s,i] = Vector3.forward;
		for (int i = 0; i < offsetsSpellCount_right[s]; i++) offsetsSpell_right[s,i] = Vector3.forward;

		
		//Shield - entgegengesetzte Halbkreise die einen Kreis formen (von oben nach unten)
		s = 1;
		spellMethodName[s] = "cast_shield";
		spellGestureSizeMultiplier[s] = 3f;
		offsetsSpellCount_left[s] = 5;
		offsetsSpellCount_right[s] = 5;
		for (int i = 0; i < offsetsSpellCount_left[s]; i++) {
			Vector3 pPrev = new Vector3(-Mathf.Sin((float)i*Mathf.PI*2f/12f),Mathf.Cos((float)i*Mathf.PI*2f/12f),0);
			Vector3 pNew = new Vector3(-Mathf.Sin((float)(i+1)*Mathf.PI*2f/12f),Mathf.Cos((float)(i+1)*Mathf.PI*2f/12f),0);
			offsetsSpell_left[s,i] = Vector3.Normalize(pNew - pPrev);
		}
		for (int i = 0; i < offsetsSpellCount_right[s]; i++) {
			Vector3 pPrev = new Vector3(Mathf.Sin((float)i*Mathf.PI*2f/12f),Mathf.Cos((float)i*Mathf.PI*2f/12f),0);
			Vector3 pNew = new Vector3(Mathf.Sin((float)(i+1)*Mathf.PI*2f/12f),Mathf.Cos((float)(i+1)*Mathf.PI*2f/12f),0);
			offsetsSpell_right[s,i] = Vector3.Normalize(pNew - pPrev);
		}
		
		//Teleport - Leichter Bogen nach vorne (oben herum)
		s = 2;
		spellMethodName[s] = "cast_teleport";
		spellGestureSizeMultiplier[s] = 1f;
		offsetsSpellCount_left[s] = 5;
		offsetsSpellCount_right[s] = 5;
		for (int i = 0; i < offsetsSpellCount_left[s]; i++) {
			Vector3 pPrev = new Vector3(0,0.8f*Mathf.Sin((float)i*Mathf.PI*2f/12f),0);
			Vector3 pNew = new Vector3(0,0.8f*Mathf.Sin((float)(i+1)*Mathf.PI*2f/12f),0.5f);
			offsetsSpell_left[s,i] = Vector3.Normalize(pNew - pPrev);
		}
		for (int i = 0; i < offsetsSpellCount_right[s]; i++) {
			Vector3 pPrev = new Vector3(0,0.8f*Mathf.Sin((float)i*Mathf.PI*2f/12f),0);
			Vector3 pNew = new Vector3(0,0.8f*Mathf.Sin((float)(i+1)*Mathf.PI*2f/12f),0.5f);
			offsetsSpell_right[s,i] = Vector3.Normalize(pNew - pPrev);
		}

		//Giant - bewegung nach außen, dann nach unten
		s = 3;
		spellMethodName[s] = "cast_giant";
		spellGestureSizeMultiplier[s] = 2f;
		offsetsSpellCount_left[s] = 4;
		offsetsSpellCount_right[s] = 4;
		for (int i = 0; i < offsetsSpellCount_left[s]/2; i++) offsetsSpell_left[s,i] = -Vector3.right;
		for (int i = 0; i < offsetsSpellCount_right[s]/2; i++) offsetsSpell_right[s,i] = Vector3.right;
		for (int i = offsetsSpellCount_left[s]/2; i < offsetsSpellCount_left[s]; i++) offsetsSpell_left[s,i] = Vector3.up;
		for (int i = offsetsSpellCount_right[s]/2; i < offsetsSpellCount_right[s]; i++) offsetsSpell_right[s,i] = Vector3.up;

	}

	void cast_fireball() {
		print("fireball");
		Quaternion angle = camTransform.rotation;
		Network.Instantiate(spellMisslePrefab,camTransform.position,angle,0);
	}
	
	void cast_shield() {
		print("shield");
	}
	
	void cast_teleport() {
		print("teleport");
	}

	void cast_giant() {
		localPlayerController.giantPlayer(10f);
		print("giant");
	}
	
}
