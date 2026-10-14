using UnityEngine;

namespace RenderHeads.Media.AVProVideo.Demos
{
	[RequireComponent(typeof(Transform))]
	public class AutoRotate : MonoBehaviour
	{
		private float x;

		private float y;

		private float z;

		private float _timer;

		private void Awake()
		{
			Randomise();
		}

		private void Update()
		{
			base.transform.Rotate(x * Time.deltaTime, y * Time.deltaTime, z * Time.deltaTime);
			_timer -= Time.deltaTime;
			if (_timer <= 0f)
			{
				Randomise();
			}
		}

		private void Randomise()
		{
			float num = 32f;
			x = Random.Range(0f - num, num);
			y = Random.Range(0f - num, num);
			z = Random.Range(0f - num, num);
			_timer = Random.Range(5f, 10f);
		}
	}
}
