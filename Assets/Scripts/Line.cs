using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Line : MonoBehaviour {

	Camera camera;
	List<Vector4> pointsLeftHand;
	bool drawing = false;
	float drawingTime = 0;
	float drawingPointDistance = 0.5f;

	bool allowChecking = true; //only check if mouse has been released before
	bool checking = false;
	float checkingTime = 0;
	int checkingPointIndex = 0;
	bool checkingPointIndexIncreased = true; //So index doesn't increase multiple times while coordinates beeing outside of TolderatedTimeInaccuracy
	Vector3 checkingWorldSpaceOffsetStart = Vector3.zero;
	float checkingWorldSpaceDistanceCurrentPoint = 999;
	public float checkingToleratedTimeInaccuracy = 0.2f;
	public float checkingMaximumWorldSpaceOffset = 0.5f;
	public bool allowFasterCasting = true; //if true, the user can cast as fast as he wants (and doesnt have to cast as slow as the drawing speed was)
	public bool requireHoldLastPoint = false; //(only!) if allowFasterCasting is OFF, determines whether the user has to hold the last point for [TimeInaccuracy] seconds

	Transform spellPatricleTransform;

	public Transform spellMisslePrefab;

	static Material lineMaterial;
	static void CreateLineMaterial ()
	{
		if (!lineMaterial)
		{
			// Unity has a built-in shader that is useful for drawing
			// simple colored things.
			var shader = Shader.Find ("Hidden/Internal-Colored");
			lineMaterial = new Material (shader);
			lineMaterial.hideFlags = HideFlags.HideAndDontSave;
			// Turn on alpha blending
			lineMaterial.SetInt ("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			lineMaterial.SetInt ("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			// Turn backface culling off
			lineMaterial.SetInt ("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			// Turn off depth writes
			lineMaterial.SetInt ("_ZWrite", 0);
		}
	}

	public void Awake() {
		camera = GetComponent<Camera>();
		spellPatricleTransform = GameObject.Find("SpellCoordinate").transform;
		spellPatricleTransform.gameObject.SetActive(false);
	}
	
	public void OnRenderObject () {
		drawLine();
	}

	public void drawLine() {
		if (!(pointsLeftHand == null) && pointsLeftHand.Count > 1)
		{
			CreateLineMaterial ();
			// Apply the line material
			lineMaterial.SetPass (0);
			GL.Begin(GL.LINES);
			for (int i = 0; i < pointsLeftHand.Count-1; i++)
			{
				Vector4 p1 = pointsLeftHand[i];
				Vector4 p2 = pointsLeftHand[i+1];
				GL.Vertex(new Vector3(p1.x,p1.y,p1.z));
				GL.Vertex(new Vector3(p2.x,p2.y,p2.z));
			}
			GL.End();
		}

	}

	public void GLTestLine() {
		CreateLineMaterial ();
		// Apply the line material
		lineMaterial.SetPass (0);
		GL.Color(Color.red); //No effect on lines
		GL.Begin(GL.LINES);
		float z = 0;
		float y = 0;
		int segmentCount = 10;
		for (int i = 0; i < segmentCount; i++)
		{
			if (i>0) GL.Vertex(new Vector3(0,y,z));
			z = i/(float)segmentCount*3f;
			y = Mathf.Sin(Mathf.PI*2f*i/(float)segmentCount);
			if (i>0) GL.Vertex(new Vector3(0,y,z));
		}
		//		GL.Vertex(new Vector3(0,0,0));
		//		GL.Vertex(new Vector3(0,2,2));
		//		GL.Vertex(new Vector3(3,2,2));
		//		GL.Vertex(new Vector3(3,2,8));
		GL.End();
	}

	Vector3 getCurrentCoordinates(bool leftHand = true) {
		float aspect = (Screen.width / Screen.height) * 0.78f;
		float lineForwardDistance = 2f;
		float factor = lineForwardDistance * 1.4f * 1.3f;
		return camera.transform.position + camera.transform.forward*lineForwardDistance + camera.transform.right*(Input.mousePosition.x-Screen.width/2)/Screen.width*factor*aspect + camera.transform.up*(Input.mousePosition.y-Screen.height/2)/Screen.height*factor/aspect;
	}

	void recordLine() {
		if (!drawing) 
		{
			//Begin new point list
			pointsLeftHand = new List<Vector4>();
			drawing = true;
			drawingTime = 0;
			print("draw start");
		}
		if (drawing)
		{
			drawingTime += Time.deltaTime;
			Vector3 currentCoordinate = getCurrentCoordinates();
//			Vector3 currentCoordinate = Camera.main.ScreenToWorldPoint(Input.mousePosition); //Current cursor/hydra point
//			print(Input.mousePosition + " ----- " + currentCoordinate.ToString());
			Vector4 currentPoint = new Vector4(currentCoordinate.x,currentCoordinate.y,currentCoordinate.z,drawingTime);
			if (pointsLeftHand.Count == 0 || Vector4.Distance(pointsLeftHand[pointsLeftHand.Count-1],currentPoint) > drawingPointDistance)
			{
				//Add new point to the list
				pointsLeftHand.Add(currentPoint);
				print("point added: " + currentPoint.ToString());
			}
		}
	}

	void checkLine() {
		Vector3 currentCoordinate = getCurrentCoordinates();
		if (!checking && (!(pointsLeftHand == null) && pointsLeftHand.Count > 1))
		{
			checking = true;
			checkingTime = 0;
			checkingPointIndex = 1;
			bool checkingPointIndexIncreased = true;
			checkingWorldSpaceOffsetStart = (Vector3)pointsLeftHand[0] - currentCoordinate;
			checkingWorldSpaceDistanceCurrentPoint = 999;
			print ("check start");
			spellPatricleTransform.gameObject.SetActive(true);
		}
		if (checking)
		{
//			currentCoordinate -= checkingWorldSpaceOffsetStart;
			spellPatricleTransform.position = currentCoordinate;
			checkingTime += Time.deltaTime;
			float temp_timeOffset = checkingTime - pointsLeftHand[checkingPointIndex].w;
			if (!checkingPointIndexIncreased && (temp_timeOffset >= checkingToleratedTimeInaccuracy || allowFasterCasting || (!requireHoldLastPoint && checkingPointIndex == pointsLeftHand.Count-1))) 
			{
				if (checkingWorldSpaceDistanceCurrentPoint < checkingMaximumWorldSpaceOffset)
				{
					if (checkingPointIndex >= pointsLeftHand.Count-1)
					{
						//Casting finished
						endSpellChecking();
						fireSpell();
					} else {
						//Continue spell casting
						checkingPointIndex++;
						checkingPointIndexIncreased = true;
						checkingWorldSpaceDistanceCurrentPoint = 999;
					}
				} else {
					//Movement was too inaccurate - abort spell
					if (temp_timeOffset >= checkingToleratedTimeInaccuracy)
					{
						endSpellChecking();
						print ("too inaccurate, try again!");
					}
				}
			}
			if (Mathf.Abs(temp_timeOffset) < checkingToleratedTimeInaccuracy || allowFasterCasting)
			{
				checkingPointIndexIncreased = false;
				float temp_WorldSpaceOffset = Vector3.Distance(currentCoordinate+checkingWorldSpaceOffsetStart, (Vector3)pointsLeftHand[checkingPointIndex]); //Vector4.Distance would also work, but time is already taken into account by ToleratedTimeInaccuracy
				if (temp_WorldSpaceOffset < checkingWorldSpaceDistanceCurrentPoint) checkingWorldSpaceDistanceCurrentPoint = temp_WorldSpaceOffset;
				print ("p:"+checkingPointIndex+"  spaceOffset: "+temp_WorldSpaceOffset);
			}

		}
	}

	void endSpellChecking() {
		checking = false;
		spellPatricleTransform.gameObject.SetActive(false);
		allowChecking = false;
	}

	// Update is called once per frame
	void Update () {
		if (Input.GetMouseButton(0))
		{
			recordLine();
		} else 
			drawing = false;
		if (Input.GetMouseButton(1))
		{
			if (allowChecking) checkLine();
		} else {
			if (checking) endSpellChecking();
			allowChecking = true;
		}
	}

	void fireSpell() {
		print("wooosh spell fired");
		Quaternion angle = transform.rotation;
		angle.SetFromToRotation(transform.position,transform.right);
		Instantiate(spellMisslePrefab,transform.position,angle);
	}
}
