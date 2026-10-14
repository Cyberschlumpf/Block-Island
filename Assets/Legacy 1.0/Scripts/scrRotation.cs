using System;
using UnityEngine;

[Serializable]
public class scrRotation : MonoBehaviour
{
	public bool rotate;

	public GameObject target;

	public float rotationSp;

	public scrRotation()
	{
		rotate = true;
		rotationSp = 15f;
	}

	public virtual void Start()
	{
	}

	public virtual void Update()
	{
		if (rotate)
		{
			target.transform.Rotate(0f, 0f, rotationSp * Time.deltaTime);
		}
	}

	public virtual void Main()
	{
	}
}
