using UnityEngine;

public class Waypoint : MonoBehaviour
{
	public Color color;

	public float radius = 0.25f;

	public string iconName;

	public float delay;

	public float angle;

	public string callFunction;

	public string callExitFunction;

	public float newMoverSpeed;

	private void OnDrawGizmos()
	{
		Gizmos.color = color;
		Gizmos.DrawSphere(base.transform.position, radius);
		if (iconName != string.Empty)
		{
			Gizmos.DrawIcon(new Vector3(base.transform.position.x, base.transform.position.y + radius * 1.5f, base.transform.position.z), iconName, true);
		}
	}
}
