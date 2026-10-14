using UnityEngine;
using System.Collections;

public class CameraObjectViewer : MonoBehaviour {
	
	public Transform target;
	
	private Vector3 angles;
	private Quaternion smoothRotation;
	
	private float distance = 30;
	private float smoothDistance = 30;
    public float minDistance = 10;
    public float maxDistance = 50;
	
	void OnEnable() {
		angles = transform.eulerAngles;
		smoothRotation = Quaternion.Euler(angles);

        if(target == null) enabled = false;
	}
	
	
	void Update () {
		if(Input.GetMouseButton(1)) {
			float dx = Input.GetAxis("Mouse X");
			float dy = Input.GetAxis("Mouse Y");
			angles.x -= dy * 5;
			angles.y += dx * 5;
			angles.x = Mathf.Clamp(angles.x, -85, 85);
		}
		smoothRotation = Quaternion.Slerp(smoothRotation, Quaternion.Euler(angles), Time.deltaTime*20f);
		
		distance += Input.GetAxis("Mouse ScrollWheel") * 10f;
        distance = Mathf.Clamp( distance, minDistance, maxDistance );
		smoothDistance = Mathf.Lerp(smoothDistance, distance, Time.deltaTime*10f);
		
		transform.position = target.position;
		transform.rotation = smoothRotation;
		transform.Translate(0, 0, -smoothDistance);
	}
	
	
}
