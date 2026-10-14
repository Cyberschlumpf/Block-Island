using System;
using UnityEngine;

public class WaypointMover : MonoBehaviour
{
	public enum FollowType
	{
		Simple = 0,
		Facing = 1,
		SmoothFacing = 2,
		SmoothDamping = 3,
		Simple2D = 4,
		Facing2D = 5,
		SmoothFacing2D = 6,
		Teleport = 7,
		InvertedFacing = 8
	}

	public enum LoopType
	{
		Once = 0,
		Cycled = 1,
		PingPong = 2,
		SeveralTimes = 3
	}

	[Serializable]
	public struct UsedAxis
	{
		public bool x;

		public bool y;

		public bool z;
	}

	public WaypointsHolder waypointsHolder;

	public FollowType followingType;

	public LoopType loopingType;

	public bool MoveOnWayImmediately;

	public bool StartFromNearestWaypoint;

	public UsedAxis ignorePositionAtAxis;

	public float damping = 3f;

	public float movementSpeed = 5f;

	public float waypointActivationDistance = 1f;

	public int numberOfLoops;

	public float preventCollisionDistance;

	public bool smoothCollisionPreventing;

	public bool dynamicWaypointsUpdate;

	private int currentWaypoint;

	private int direction = 1;

	private Vector3 velocity = Vector3.zero;

	private Vector3 targetPosition;

	private float delayTillTime;

	private int loopNumber = 1;

	private bool inMove;

	private bool suspended;

	private int previousWaypoint;

	private float initialMovementSpeed;

	private float changedMovementSpeed;

	private bool callExitFunction;

	private bool onWaypoint;

	private void Start()
	{
		if (waypointsHolder == null)
		{
			Debug.LogWarning("No WaypointsHolder attached to " + base.gameObject.name);
			return;
		}
		if ((bool)GetComponent<Rigidbody>())
		{
			GetComponent<Rigidbody>().freezeRotation = true;
		}
		changedMovementSpeed = (initialMovementSpeed = movementSpeed);
		if (StartFromNearestWaypoint)
		{
			int num = 0;
			float num2 = float.PositiveInfinity;
			for (int i = 0; i < waypointsHolder.waypoints.Count; i++)
			{
				Vector3 position = waypointsHolder.waypoints[i].gameObject.transform.position;
				position = IgnorePositionByAxis(position);
				float num3 = Vector3.Distance(base.transform.position, position);
				if (num3 < num2)
				{
					num = i;
					num2 = num3;
				}
			}
			currentWaypoint = num;
		}
		else
		{
			currentWaypoint = 0;
		}
		if (MoveOnWayImmediately)
		{
			base.transform.position = waypointsHolder.waypoints[currentWaypoint].gameObject.transform.position;
		}
		targetPosition = waypointsHolder.waypoints[currentWaypoint].gameObject.transform.position;
		targetPosition = IgnorePositionByAxis(targetPosition);
	}

	private void Update()
	{
		if (waypointsHolder == null || currentWaypoint < 0)
		{
			inMove = false;
			return;
		}
		bool flag = false;
		Vector3 position = base.transform.position;
		Vector3 point = position;
		if (preventCollisionDistance > 0f)
		{
			RaycastHit hitInfo;
			if (Physics.CapsuleCast(position, point, 0.5f, base.transform.forward, out hitInfo, preventCollisionDistance))
			{
				if (!smoothCollisionPreventing)
				{
					flag = true;
				}
				else
				{
					movementSpeed = changedMovementSpeed * Vector3.Distance(base.transform.position, hitInfo.point) / preventCollisionDistance;
					if (movementSpeed < changedMovementSpeed / preventCollisionDistance)
					{
						flag = true;
					}
				}
			}
			else
			{
				movementSpeed = changedMovementSpeed;
			}
		}
		if (!suspended && !flag && currentWaypoint >= 0 && delayTillTime < Time.time)
		{
			inMove = true;
			if (dynamicWaypointsUpdate && currentWaypoint >= 0 && targetPosition != waypointsHolder.waypoints[currentWaypoint].gameObject.transform.position)
			{
				targetPosition = waypointsHolder.waypoints[currentWaypoint].gameObject.transform.position;
				targetPosition = IgnorePositionByAxis(targetPosition);
			}
			if (Vector3.Distance(base.transform.position, targetPosition) < waypointActivationDistance)
			{
				if (waypointsHolder.waypoints[currentWaypoint].delay > 0f)
				{
					delayTillTime = Time.time + waypointsHolder.waypoints[currentWaypoint].delay;
				}
				if (waypointsHolder.waypoints[currentWaypoint].callFunction != string.Empty)
				{
					SendMessage(waypointsHolder.waypoints[currentWaypoint].callFunction, SendMessageOptions.DontRequireReceiver);
				}
				if (waypointsHolder.waypoints[currentWaypoint].newMoverSpeed > 0f)
				{
					ChangeWaypointMoverSpeed(waypointsHolder.waypoints[currentWaypoint].newMoverSpeed);
				}
				previousWaypoint = currentWaypoint;
				currentWaypoint += direction;
				onWaypoint = true;
				if (currentWaypoint > waypointsHolder.waypoints.Count - 1 || currentWaypoint < 0)
				{
					switch (loopingType)
					{
					case LoopType.Once:
						currentWaypoint = -1;
						return;
					case LoopType.Cycled:
						currentWaypoint = ((currentWaypoint < 0) ? (waypointsHolder.waypoints.Count - 1) : 0);
						break;
					case LoopType.PingPong:
						direction = -direction;
						currentWaypoint += direction;
						break;
					case LoopType.SeveralTimes:
						if (loopNumber < numberOfLoops)
						{
							currentWaypoint = 0;
							loopNumber++;
							break;
						}
						currentWaypoint = -1;
						return;
					}
				}
				if (currentWaypoint >= 0 && (bool)waypointsHolder.waypoints[currentWaypoint])
				{
					targetPosition = waypointsHolder.waypoints[currentWaypoint].gameObject.transform.position;
					targetPosition = IgnorePositionByAxis(targetPosition);
				}
				else if (currentWaypoint < waypointsHolder.waypoints.Count && currentWaypoint >= 0)
				{
					currentWaypoint -= direction;
					Debug.LogWarning("Waypoint is missed in " + waypointsHolder.gameObject.name);
				}
				callExitFunction = true;
			}
			else
			{
				onWaypoint = false;
				if (waypointsHolder.waypoints[previousWaypoint].callExitFunction != string.Empty && callExitFunction && Vector3.Distance(base.transform.position, waypointsHolder.waypoints[previousWaypoint].gameObject.transform.position) < waypointActivationDistance)
				{
					SendMessage(waypointsHolder.waypoints[previousWaypoint].callExitFunction, SendMessageOptions.DontRequireReceiver);
					callExitFunction = false;
				}
			}
			if (followingType == FollowType.SmoothFacing)
			{
				Quaternion b = Quaternion.LookRotation(targetPosition - base.transform.position);
				base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, Time.deltaTime * damping);
				base.transform.Translate(Vector3.forward * movementSpeed * Time.deltaTime);
			}
			if (followingType == FollowType.Facing)
			{
				base.transform.LookAt(targetPosition);
				base.transform.Translate(Vector3.forward * movementSpeed * Time.deltaTime);
			}
			if (followingType == FollowType.InvertedFacing)
			{
				base.transform.LookAt(targetPosition);
				base.transform.Rotate(Vector3.up, 180f);
				base.transform.Translate(-Vector3.forward * movementSpeed * Time.deltaTime);
			}
			if (followingType == FollowType.Simple)
			{
				base.transform.position = Vector3.MoveTowards(base.transform.position, targetPosition, movementSpeed * Time.deltaTime);
			}
			if (followingType == FollowType.SmoothDamping)
			{
				base.transform.position = Vector3.SmoothDamp(base.transform.position, targetPosition, ref velocity, movementSpeed);
			}
			if (followingType == FollowType.Simple2D)
			{
				base.transform.position = Vector2.MoveTowards(base.transform.position, targetPosition, movementSpeed * Time.deltaTime);
			}
			if (followingType == FollowType.Facing2D)
			{
				Vector2 vector = targetPosition - base.transform.position;
				float num = Vector2.Angle(vector, base.transform.right);
				if (num > 3f)
				{
					base.transform.eulerAngles = new Vector3(base.transform.rotation.eulerAngles.x, base.transform.rotation.eulerAngles.y, base.transform.rotation.eulerAngles.z - num);
				}
				base.transform.Translate(Vector3.right * movementSpeed * Time.deltaTime);
			}
			if (followingType == FollowType.SmoothFacing2D)
			{
				Vector3 vector2 = targetPosition - base.transform.position;
				float num2 = Vector3.Angle(vector2, base.transform.right);
				if (num2 > 3f)
				{
					base.transform.eulerAngles = new Vector3(base.transform.rotation.eulerAngles.x, base.transform.rotation.eulerAngles.y, Mathf.Lerp(base.transform.rotation.eulerAngles.z, base.transform.rotation.eulerAngles.z - num2, damping * Time.deltaTime));
				}
				base.transform.Translate(Vector3.right * movementSpeed * Time.deltaTime);
			}
			if (followingType == FollowType.Teleport)
			{
				base.transform.position = targetPosition;
			}
			if (followingType != FollowType.Simple2D && followingType != FollowType.Facing2D && followingType != FollowType.SmoothFacing2D)
			{
				float num3 = Vector3.Distance(waypointsHolder.waypoints[previousWaypoint].gameObject.transform.position, waypointsHolder.waypoints[currentWaypoint].gameObject.transform.position);
				num3 = Vector3.Distance(base.transform.position, targetPosition) / num3;
				base.transform.localEulerAngles = new Vector3(base.transform.localEulerAngles.x, base.transform.localEulerAngles.y, Mathf.Lerp(waypointsHolder.waypoints[previousWaypoint].angle, waypointsHolder.waypoints[currentWaypoint].angle, 1f - num3));
			}
		}
		else
		{
			inMove = false;
		}
	}

	public void ReverseDirection()
	{
		previousWaypoint = currentWaypoint;
		direction = -direction;
		currentWaypoint = ((direction <= 0) ? (currentWaypoint - 1) : (currentWaypoint + 1));
		if (currentWaypoint < 0)
		{
			currentWaypoint = waypointsHolder.waypoints.Count - 1;
		}
		else if (currentWaypoint > waypointsHolder.waypoints.Count - 1)
		{
			currentWaypoint = 0;
		}
		targetPosition = waypointsHolder.waypoints[currentWaypoint].gameObject.transform.position;
	}

	public void SetDirection(int _direction)
	{
		previousWaypoint = currentWaypoint;
		currentWaypoint = ((_direction <= 0) ? (currentWaypoint - 1) : (currentWaypoint + 1));
		if (currentWaypoint < 0)
		{
			currentWaypoint = waypointsHolder.waypoints.Count - 1;
		}
		else if (currentWaypoint > waypointsHolder.waypoints.Count - 1)
		{
			currentWaypoint = 0;
		}
		targetPosition = waypointsHolder.waypoints[currentWaypoint].gameObject.transform.position;
	}

	public bool IsOnWaypoint()
	{
		return onWaypoint;
	}

	public void ReturnToPreviousWaypoint()
	{
		currentWaypoint = previousWaypoint;
		base.transform.position = waypointsHolder.waypoints[previousWaypoint].gameObject.transform.position;
		targetPosition = waypointsHolder.waypoints[previousWaypoint].gameObject.transform.position;
	}

	public bool isMoving()
	{
		return inMove;
	}

	public void Suspend(bool state)
	{
		suspended = state;
	}

	public void Pause()
	{
		suspended = true;
	}

	public void Unpause()
	{
		suspended = false;
	}

	public void ChangeWaypointMoverSpeed(float newSpeed)
	{
		movementSpeed = newSpeed;
		changedMovementSpeed = movementSpeed;
	}

	public Vector3 IgnorePositionByAxis(Vector3 positionToUpdate)
	{
		Vector3 result = positionToUpdate;
		if (ignorePositionAtAxis.x)
		{
			result.x = base.transform.position.x;
		}
		if (ignorePositionAtAxis.y)
		{
			result.y = base.transform.position.y;
		}
		if (ignorePositionAtAxis.z)
		{
			result.z = base.transform.position.z;
		}
		return result;
	}

	public void SmoothLookAt2D(Transform objectTransform, Vector2 targetPosition, float smoothingValue)
	{
		Vector3 vector = objectTransform.InverseTransformPoint(targetPosition);
		float b = Mathf.Atan2(vector.y, vector.x) * 57.29578f;
		objectTransform.Rotate(0f, 0f, Mathf.LerpAngle(0f, b, Time.deltaTime * smoothingValue));
	}
}
