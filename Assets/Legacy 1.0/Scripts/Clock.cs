using UnityEngine;

// Block Island 1.0 wall clock. Shows the real Central European time (Europe/Berlin, summer time included).
public class Clock : MonoBehaviour
{
	public int minutes;

	public int hour;

	public float clockSpeed = 1f;

	// sign of the local z rotation that turns the hands clockwise as seen from the front
	const float HandDirection = 1f;

	private GameObject pointerSeconds;

	private GameObject pointerMinutes;

	private GameObject pointerHours;

	private void Start()
	{
		pointerSeconds = base.transform.Find("rotation_axis_pointer_seconds").gameObject;
		pointerMinutes = base.transform.Find("rotation_axis_pointer_minutes").gameObject;
		pointerHours = base.transform.Find("rotation_axis_pointer_hour").gameObject;
	}

	private void Update()
	{
		System.DateTime now = BlockIslandRealTime.CentralEurope;
		float s = now.Second + now.Millisecond / 1000f;
		hour = now.Hour; minutes = now.Minute;
		float z = 6f * Mathf.Floor(s);
		float z2 = 6f * minutes + .1f * s;
		float z3 = 30f * (hour % 12) + 0.5f * minutes;
		pointerSeconds.transform.localEulerAngles = new Vector3(0f, 0f, HandDirection * z);
		pointerMinutes.transform.localEulerAngles = new Vector3(0f, 0f, HandDirection * z2);
		pointerHours.transform.localEulerAngles = new Vector3(0f, 0f, HandDirection * z3);
	}
}
