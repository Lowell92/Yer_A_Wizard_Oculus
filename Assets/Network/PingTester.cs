using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PingTester : MonoBehaviour {

	public float speed = 100f;
	NetworkView networkView;
	RectTransform rt;
	bool pingTestRunning = false;
	
	void Start () {
		networkView = GetComponent<NetworkView>();
		rt = GetComponent<RectTransform>();
		InvokeRepeating("isMineColor",1f,1f);
	}

	void isMineColor() {
		if (networkView.isMine) GetComponent<RawImage>().color = Color.red;
		else GetComponent<RawImage>().color = Color.black;
	}

	void Update() {
		if (Input.GetKeyUp(KeyCode.Space))
		{
			startPingTest();
		}
		if (rt.position.x > Screen.width-10) pingTestRunning = false;
		if (pingTestRunning) 
		{
			rt.position = new Vector3(rt.position.x+speed*Time.deltaTime,rt.position.y,rt.position.z);
		}
	}

	void startPingTest() {
		rt.position = new Vector3(10f,rt.position.y,rt.position.z);
		pingTestRunning = true;
	}

	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		float syncPositionX = 0;

		if ((networkView.isMine || Network.isServer) && stream.isWriting)
		{
			//Write data to stream
			syncPositionX = GetComponent<RectTransform>().position.x;
			stream.Serialize(ref syncPositionX);
		}
		else
		{
			//Get data from stream
			stream.Serialize(ref syncPositionX);
			rt.position = new Vector3(syncPositionX,rt.position.y,rt.position.z);
		}
	}
}
