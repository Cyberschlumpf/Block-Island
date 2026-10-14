using UnityEngine;

public class Clock : MonoBehaviour
{
	public int minutes;

	public int hour;

	public float clockSpeed = 1f;

	private int seconds;

	private float msecs;

	private GameObject pointerSeconds;

	private GameObject pointerMinutes;

	private GameObject pointerHours;

	private void Start()
	{
		pointerSeconds = base.transform.Find("rotation_axis_pointer_seconds").gameObject;
		pointerMinutes = base.transform.Find("rotation_axis_pointer_minutes").gameObject;
		pointerHours = base.transform.Find("rotation_axis_pointer_hour").gameObject;
		msecs = 0f;
		seconds = 0;
	}

	private void Update()
	{
		msecs += Time.deltaTime * clockSpeed;
		if (msecs >= 1f)
		{
			msecs--;
			seconds++;
			if (seconds >= 60)
			{
				seconds = 0;
				minutes++;
				if (minutes > 60)
				{
					minutes = 0;
					hour++;
					if (hour >= 24)
					{
						hour = 0;
					}
				}
			}
		}
		float z = 6f * (float)seconds;
		float z2 = 6f * (float)minutes;
		float z3 = 30f * (float)hour + 0.5f * (float)minutes;
		pointerSeconds.transform.localEulerAngles = new Vector3(0f, 0f, z);
		pointerMinutes.transform.localEulerAngles = new Vector3(0f, 0f, z2);
		pointerHours.transform.localEulerAngles = new Vector3(0f, 0f, z3);
	}
}
