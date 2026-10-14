using UnityEngine;

public class LightAnimator : MonoBehaviour
{
	public float speed;

	public Material material;

	public Texture[] textures;

	private float frameTime;

	private void Update()
	{
		int num = textures.Length;
		if (num != 0)
		{
			frameTime += Time.deltaTime * speed;
			material.mainTexture = textures[(int)Mathf.Abs(frameTime) % num];
			material.SetTexture("_Illum", textures[(int)Mathf.Abs(frameTime) % num]);
		}
	}
}
