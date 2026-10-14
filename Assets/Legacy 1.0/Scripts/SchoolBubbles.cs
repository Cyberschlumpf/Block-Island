using System;
using UnityEngine;

[Serializable]
public class SchoolBubbles : MonoBehaviour
{
	public ParticleSystem _bubbleParticles;

	public float _emitEverySecond;

	public float _speedEmitMultiplier;

	public int _minBubbles;

	public int _maxBubbles;

	public SchoolBubbles()
	{
		_emitEverySecond = 0.01f;
		_speedEmitMultiplier = 0.25f;
		_maxBubbles = 5;
	}

	public virtual void Start()
	{
		if (!_bubbleParticles)
		{
			transform.GetComponent(typeof(ParticleSystem));
		}
	}

	public virtual void EmitBubbles(Vector3 pos, float amount)
	{
		float num = amount * _speedEmitMultiplier;
		if (num >= 1f)
		{
			_bubbleParticles.transform.position = pos;
			_bubbleParticles.Emit((int)Mathf.Clamp(amount * _speedEmitMultiplier, _minBubbles, _maxBubbles));
		}
	}

	public virtual void Main()
	{
	}
}
