using System;
using UnityEngine;

namespace MirzaBeig.Animation
{
	[Serializable]
	public class Rotator : MonoBehaviour
	{
		public Vector3 worldRotationSpeed;

		public Vector3 localRotationSpeed;

		private void Awake()
		{
		}

		private void Start()
		{
		}

		private void Update()
		{
			base.transform.Rotate(localRotationSpeed * Time.deltaTime, Space.Self);
			base.transform.Rotate(worldRotationSpeed * Time.deltaTime, Space.World);
		}
	}
}
