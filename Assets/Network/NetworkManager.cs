// http://www.paladinstudios.com/2013/07/10/how-to-create-an-online-multiplayer-game-with-unity/
// http://docs.unity3d.com/ScriptReference/Network.html

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class NetworkManager : MonoBehaviour {

	private GameObject lobbyCanvas;
	public const int maxPlayerCount = 2;
	static public int port = 25000;
	static public string serverIP = "193.175.183.84";
	public int sendRate = 50;

	public GameObject playerPrefab;
	public GameObject playerPrefabOTHER;


	void Start () {
		Network.sendRate = sendRate;
		GameObject.Find("InputNetSendRate_Placeholder").GetComponent<Text>().text = sendRate.ToString();
		GameObject.Find("TextOwnIP").GetComponent<Text>().text = "Your IP is: " + Network.player.ipAddress;
		lobbyCanvas = GameObject.Find("UI_Network");
	}
	
	public void StartServer()
	{
//		if (!Network.isClient && !Network.isServer)
		changeLobbyStatus("Starting Server ...");
		Invoke("connectionFailed",3f);
		NetworkConnectionError e = Network.InitializeServer(maxPlayerCount, port, !Network.HavePublicAddress ());
		if (e.ToString() != "NoError") changeLobbyStatus("Starting Server: "+e.ToString());
	}

	public void JoinServer() {
//		if (!Network.isClient && !Network.isServer)
		changeLobbyStatus("Joining Server ...");
		Invoke("connectionFailed",3f);
		NetworkConnectionError e = Network.Connect(serverIP, port);
		if (e.ToString() != "NoError") changeLobbyStatus("Joining Server: "+e.ToString());
	}

	void OnConnectedToServer()
	{
		CancelInvoke("connectionFailed");
		changeLobbyStatus("Connected to server");
		SpawnPlayer();
	}

	void OnServerInitialized()
	{
		CancelInvoke("connectionFailed");
		changeLobbyStatus("Server initialized");
		SpawnPlayer();
	}

	private void SpawnPlayer()
	{
		GameObject.Find ("LobbyCamera").SetActive (false);
		if (Network.isServer)
			Network.Instantiate(playerPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity, 0);
		else
			Network.Instantiate(playerPrefabOTHER, new Vector3(0f, 0f, int.Parse(Network.player.ToString()+1)*2f), Quaternion.identity, 0);
	}

	private void connectionFailed() {
		changeLobbyStatus("Connecting failed!");
	}

	////////////////////////////////////////////
	// Lobby
	////////////////////////////////////////////

	static public void changeLobbyStatus(String newStatus) {
		GameObject.Find("TextStatus").GetComponent<Text>().text = "[Status: "+newStatus+"]";
	}

	static public void changeMenuDebugStatus(String newStatus) {
		GameObject.Find("TextDebug").GetComponent<Text>().text = "[Debug: "+newStatus+"]";
	}

	public void onServerIPInputValueChange() {
		Invoke("parseServerIPInputValue",0.05f);
	}

	void parseServerIPInputValue() {
		serverIP = GameObject.Find("InputServerIP_Text").GetComponent<Text>().text;
	}

	public void onNetSendRateInputValueChange() {
		Invoke("parseNetSendRateInputValue",0.05f);
	}

	void parseNetSendRateInputValue() {
		try {
			int newSendRate = int.Parse(GameObject.Find("InputNetSendRate_Text").GetComponent<Text>().text);
			if (newSendRate >= 1 && newSendRate < 200)
			{
				sendRate = newSendRate;
				Network.sendRate = sendRate;
				GameObject.Find("InputNetSendRate_Text").GetComponent<Text>().color = Color.black;
			} else throw new Exception();
		}
		catch (Exception e){
			GameObject.Find("InputNetSendRate_Text").GetComponent<Text>().color = Color.red;
		}
	}

	public void toggleLobby() {
		lobbyCanvas.SetActive(!lobbyCanvas.activeInHierarchy);
	}




}
