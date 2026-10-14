using System.Collections.Generic;
using UnityEngine;

public class WaypointsHolder : MonoBehaviour
{
	public Color color = new Color(0f, 1f, 0f, 0.5f);

	public List<Waypoint> waypoints = new List<Waypoint>();

	public bool colorizeWaypoints = true;

	private void Awake()
	{
		if (waypoints == null || waypoints.Count == 0)
		{
			Waypoint[] componentsInChildren = GetComponentsInChildren<Waypoint>();
			Waypoint[] array = componentsInChildren;
			foreach (Waypoint newWaypoint in array)
			{
				AddWaypoint(newWaypoint);
			}
		}
		Clean();
	}

	public void Clean()
	{
		for (int i = 0; i < waypoints.Count; i++)
		{
			if (waypoints[i] == null)
			{
				waypoints.RemoveAt(i);
				i--;
			}
		}
	}

	public void AddWaypoint(Waypoint _newWaypoint)
	{
		if (colorizeWaypoints)
		{
			_newWaypoint.color = color;
		}
		waypoints.Add(_newWaypoint);
	}

	public void CreateWaypoint(Vector3 _position, string name = "waypoint")
	{
		GameObject gameObject = new GameObject();
		gameObject.name = name;
		gameObject.transform.parent = base.transform;
		gameObject.transform.position = _position;
		AddWaypoint(gameObject.AddComponent<Waypoint>());
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = color;
		if (waypoints.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < waypoints.Count - 1; i++)
		{
			if ((bool)waypoints[i] && (bool)waypoints[i + 1])
			{
				Gizmos.DrawLine(waypoints[i].gameObject.transform.position, waypoints[i + 1].gameObject.transform.position);
				if (colorizeWaypoints)
				{
					waypoints[i + 1].color = color;
				}
			}
		}
	}
}
