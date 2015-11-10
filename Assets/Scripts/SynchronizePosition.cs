using UnityEngine;
using System.Collections;

public class SynchronizePosition : MonoBehaviour {

	NetworkView networkView;

	//Movement Interpolation
	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private Vector3 syncStartPosition = Vector3.zero;
	private Vector3 syncEndPosition = Vector3.zero;

	void Start() {
		networkView = GetComponent<NetworkView>();
	}

	void Update() {
		//Interpolate from last to this screenshot
		if (!networkView.isMine) transform.position = Vector3.Lerp (syncStartPosition, syncEndPosition, (Time.time - lastSynchronizationTime) / syncDelay);
	}

	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		Vector3 syncPosition = Vector3.zero;
		if ((networkView.isMine || Network.isServer) && stream.isWriting)
		{
			//Write data to stream
			syncPosition = transform.position;
			stream.Serialize(ref syncPosition);
		}
		else
		{
			//Get data from stream
			stream.Serialize(ref syncPosition);
			
			//Calculate sync delay (ping, kind of)
			syncDelay = Time.time - lastSynchronizationTime;
			lastSynchronizationTime = Time.time;
			NetworkManager.changeMenuDebugStatus("syncDelay: "+syncDelay);
			
			//Set new start and end position for movement interpolation
			syncStartPosition = syncEndPosition;
			syncEndPosition = syncPosition;
		}
	}
}
