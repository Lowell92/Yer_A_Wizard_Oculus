// http://docs.unity3d.com/ScriptReference/NetworkView.html

using UnityEngine;
using System.Collections;

public class PlayerControllerNetwork : MonoBehaviour {

	public float speed = 10f;
	GameObject model;
	Rigidbody rigidbody;
	NetworkView networkView;

	//Movement Interpolation
	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private float syncTime = 0f;
	private Vector3 syncStartPosition = Vector3.zero;
	private Vector3 syncEndPosition = Vector3.zero;

	void Start() {
		foreach(Transform t in gameObject.GetComponentsInChildren<Transform>())
			if (t.gameObject.name == "Model") model = t.gameObject;
		rigidbody = model.GetComponent<Rigidbody>();
		networkView = GetComponent<NetworkView>();
		if (networkView.isMine) GetComponentInChildren<MeshRenderer>().material.color = Color.red;
	}

	void Update()
	{
		if (networkView.isMine) 
		{
			InputMovement();
		} else {
			SyncedMovement();
		}
	}

	private void SyncedMovement()
	{
		syncTime += Time.deltaTime;
		rigidbody.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
	}
	
	void InputMovement()
	{
		if (Input.GetAxis("Vertical") > 0)
			rigidbody.MovePosition(rigidbody.position + Vector3.forward * speed * Time.deltaTime);
		
		if (Input.GetAxis("Vertical") < 0)
			rigidbody.MovePosition(rigidbody.position - Vector3.forward * speed * Time.deltaTime);
		
		if (Input.GetAxis("Horizontal") > 0)
			rigidbody.MovePosition(rigidbody.position + Vector3.right * speed * Time.deltaTime);
		
		if (Input.GetAxis("Horizontal") < 0)
			rigidbody.MovePosition(rigidbody.position - Vector3.right * speed * Time.deltaTime);
	}

	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		Vector3 syncPosition = Vector3.zero;
		Vector3 syncVelocity = Vector3.zero;
		Vector3 syncRotation = Vector3.zero;
		Vector3 syncAngularVelocity = Vector3.zero;
		if ((networkView.isMine || Network.isServer) && stream.isWriting)
		{
			//Write data to stream
			syncPosition = rigidbody.position;
			stream.Serialize(ref syncPosition);
			syncVelocity = rigidbody.velocity;
			stream.Serialize(ref syncVelocity);
			syncRotation = rigidbody.rotation.eulerAngles;
			stream.Serialize(ref syncRotation);
			syncAngularVelocity = rigidbody.angularVelocity;
			stream.Serialize(ref syncAngularVelocity);
		}
		else
		{
			//Get data from stream
			stream.Serialize(ref syncPosition);
			stream.Serialize(ref syncVelocity);
			stream.Serialize(ref syncRotation);
			stream.Serialize(ref syncAngularVelocity);

			//Calculate sync delay (ping, kind of)
			syncTime = 0f;
			syncDelay = Time.time - lastSynchronizationTime;
			lastSynchronizationTime = Time.time;
			NetworkManager.changeMenuDebugStatus("syncDelay: "+syncDelay);

			//Set new start and end position for movement interpolation
			syncEndPosition = syncPosition + syncVelocity * syncDelay;
			syncStartPosition = rigidbody.position;

			//Set object angle immediately (no interpolation)
			rigidbody.rotation = Quaternion.Euler(syncRotation);
			rigidbody.angularVelocity = syncAngularVelocity;
		}
	}
}
