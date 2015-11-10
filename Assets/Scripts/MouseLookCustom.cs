using UnityEngine;
using System.Collections;
using UnityEngine.VR;

public class MouseLookCustom : MonoBehaviour {

    private Vector3 mousePrevious;
    private GameObject cam;
	public float sensivity = 0.2f;
	public bool forceActive = true;

    void Awake()
    {
        // Insert name of the camera to be controlled
        cam = GameObject.Find("Main Camera");
    }

    void Update()
    {
        //Mouse look if VR is disabled
		if (!VRDevice.isPresent || forceActive)
        {
            Vector3 mouseCurrent = Input.mousePosition;
            Vector3 mouseDelta = mouseCurrent - mousePrevious;
            mousePrevious = mouseCurrent;
            cam.transform.RotateAround(cam.transform.position, Vector3.up, mouseDelta.x*sensivity);
			cam.transform.RotateAround(cam.transform.position, cam.transform.right, -mouseDelta.y*sensivity);
        }
    }
}
