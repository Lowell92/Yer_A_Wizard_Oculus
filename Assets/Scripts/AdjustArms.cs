using UnityEngine;
using System.Collections;

public class AdjustArms : MonoBehaviour {

	public Transform snapTarget;

	Transform leftHand, rightHand;
	MeshFilter meshFilter;
	Vector3[] vertices;
	Vector2[] uvs;
	int[] triangles;
	
	Vector3 faceOriginAtHand;
	Vector3[] verticesOriginal;

	
	void Start () {
		//Find hands of this player //TODO bugged - only executed for other player's arms ?!
//		int i = -1;
//		do {
//			i++;
//			try {
//				Transform t = transform.parent.GetChild(i);
//				string s = t.gameObject.name;
//				print(s + "        gameObject: "+gameObject);
//				if (s == "Hand - Left") leftHand = t;
//				if (s == "Hand - Right") rightHand = t;
//				if (s == "HandsController") 
//				{
//					if (t.GetComponentsInChildren<Transform>()[0].gameObject.name == "Hand - Right")
//					{
//					    rightHand = t.GetComponentsInChildren<Transform>()[0];
//					    leftHand = t.GetComponentsInChildren<Transform>()[1];
//					} else {
//						rightHand = t.GetComponentsInChildren<Transform>()[1];
//						leftHand = t.GetComponentsInChildren<Transform>()[0];
//					}
//				}
//			}
//			catch (System.Exception e) {
//				i = 20;
//			}
//		} while ((leftHand == null || rightHand == null) && i < 20);

//		Transform[] childTransforms = transform.parent.GetChild(transform.GetSiblingIndex()+1);
//		print("PARENT: "+transform.parent);
//
//		foreach (Transform t in childTransforms) 
//		{
//			print(t.gameObject.name+", PARENT: "+transform.parent);
//			if (t.gameObject.name == "Hand - Left") leftHand = t;
//			if (t.gameObject.name == "Hand - Right") rightHand = t;
//		}
//		leftHand = GameObject.Find("Hand - Left").transform;
//		rightHand = GameObject.Find("Hand - Right").transform;
		meshFilter = GetComponent<MeshFilter> ();
		createBoxMesh();
	}

	void Update () {
		if (snapTarget != null) adjustMeshToHand();
	}

	void adjustMeshToHand() {
//		if (snapToLeftHand)
//			faceOriginAtHand = leftHand.transform.position - 0.1f * leftHand.transform.forward;
//		else
//			faceOriginAtHand = rightHand.transform.position - 0.1f * rightHand.transform.forward;
		faceOriginAtHand = snapTarget.transform.position - 0.1f * snapTarget.transform.forward;
		for (int i = 4; i < 8; i++) vertices[i] = transform.worldToLocalMatrix.MultiplyVector(faceOriginAtHand-transform.position)+ verticesOriginal[i]*0.5f;
		meshFilter.mesh.vertices = vertices;
	}

	void createBoxMesh() {
		Mesh mesh = new Mesh();
		meshFilter.mesh = mesh;
		vertices = new Vector3[8];
		verticesOriginal = new Vector3[8];
		uvs = new Vector2[8];
		vertices[0] = new Vector3(-0.5f,-0.5f,-0.5f);
		vertices[1] = new Vector3(0.5f,-0.5f,-0.5f);
		vertices[2] = new Vector3(-0.5f,0.5f,-0.5f);
		vertices[3] = new Vector3(0.5f,0.5f,-0.5f);
		vertices[4] = new Vector3(-0.5f,-0.5f,0.5f);
		vertices[5] = new Vector3(0.5f,-0.5f,0.5f);
		vertices[6] = new Vector3(-0.5f,0.5f,0.5f);
		vertices[7] = new Vector3(0.5f,0.5f,0.5f);
		for (int i = 0; i < 8; i++) 
		{
			uvs[i] = new Vector3(vertices[i].x*2f,vertices[i].y*2f);
			verticesOriginal[i] = vertices[i];
		}
		triangles = new int[] {2,1,0,
		1,2,3,
		2,6,3,
		7,3,6,
		0,4,2,
		6,2,4,
		3,5,1,
		5,3,7,
		1,4,0,
		4,1,5,
		5,6,4,
		6,5,7};
		//Apply
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		mesh.RecalculateNormals();
		mesh.Optimize();
	}
}
