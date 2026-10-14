using System;
using UnityEngine;

[Serializable]
public class UnluckAnimatedMesh : MonoBehaviour
{
	public MeshFilter[] meshCache;

	[HideInInspector]
	public Transform meshCached;

	public Transform meshContainerFBX;

	public float playSpeed;

	public float playSpeedRandom;

	public bool randomSpeedLoop;

	private float currentSpeed;

	[HideInInspector]
	public float currentFrame;

	[HideInInspector]
	public int meshCacheCount;

	[HideInInspector]
	public MeshFilter meshFilter;

	[HideInInspector]
	public Renderer rendererComponent;

	public float updateInterval;

	public bool randomRotateX;

	public bool randomRotateY;

	public bool randomRotateZ;

	public bool randomStartFrame;

	public bool randomRotateLoop;

	public bool loop;

	public bool pingPong;

	public bool playOnAwake;

	public Vector2 randomStartDelay;

	private float startDelay;

	private float startDelayCounter;

	[NonSerialized]
	public static float updateSeed;

	private bool pingPongToggle;

	public Transform transformCache;

	public float delta;

	public UnluckAnimatedMesh()
	{
		playSpeed = 1f;
		updateInterval = 0.05f;
		randomStartFrame = true;
		loop = true;
		playOnAwake = true;
		randomStartDelay = new Vector2(0f, 0f);
	}

	public virtual void Start()
	{
		transformCache = transform;
		CheckIfMeshHasChanged();
		startDelay = UnityEngine.Random.Range(randomStartDelay.x, randomStartDelay.y);
		updateSeed += 0.0005f;
		if (playOnAwake)
		{
			Invoke("Play", updateInterval + updateSeed);
		}
		if (!(updateSeed < updateInterval))
		{
			updateSeed = 0f;
		}
		if (!rendererComponent)
		{
			GetRequiredComponents();
		}
	}

	public virtual void Play()
	{
		CancelInvoke();
		if (randomStartFrame)
		{
			currentFrame = (float)meshCacheCount * UnityEngine.Random.value;
		}
		else
		{
			currentFrame = 0f;
		}
		meshFilter.sharedMesh = meshCache[(int)currentFrame].sharedMesh;
		enabled = true;
		RandomizePlaySpeed();
		Invoke("AnimatedMesh", updateInterval);
		RandomRotate();
	}

	public virtual void RandomRotate()
	{
		if (randomRotateX)
		{
			int num = UnityEngine.Random.Range(0, 360);
			Quaternion localRotation = transformCache.localRotation;
			Vector3 eulerAngles = localRotation.eulerAngles;
			float num2 = (eulerAngles.x = num);
			Vector3 vector = (localRotation.eulerAngles = eulerAngles);
			Quaternion quaternion = (transformCache.localRotation = localRotation);
		}
		if (randomRotateY)
		{
			int num3 = UnityEngine.Random.Range(0, 360);
			Quaternion localRotation2 = transformCache.localRotation;
			Vector3 eulerAngles2 = localRotation2.eulerAngles;
			float num4 = (eulerAngles2.y = num3);
			Vector3 vector3 = (localRotation2.eulerAngles = eulerAngles2);
			Quaternion quaternion3 = (transformCache.localRotation = localRotation2);
		}
		if (randomRotateZ)
		{
			int num5 = UnityEngine.Random.Range(0, 360);
			Quaternion localRotation3 = transformCache.localRotation;
			Vector3 eulerAngles3 = localRotation3.eulerAngles;
			float num6 = (eulerAngles3.z = num5);
			Vector3 vector5 = (localRotation3.eulerAngles = eulerAngles3);
			Quaternion quaternion5 = (transformCache.localRotation = localRotation3);
		}
	}

	public virtual void GetRequiredComponents()
	{
		rendererComponent = (Renderer)GetComponent(typeof(Renderer));
	}

	public virtual void RandomizePlaySpeed()
	{
		if (!(playSpeedRandom <= 0f))
		{
			currentSpeed = UnityEngine.Random.Range(playSpeed - playSpeedRandom, playSpeed + playSpeedRandom);
		}
		else
		{
			currentSpeed = playSpeed;
		}
	}

	public virtual void FillCacheArray()
	{
		GetRequiredComponents();
		meshFilter = (MeshFilter)transformCache.GetComponent(typeof(MeshFilter));
		meshCacheCount = meshContainerFBX.childCount;
		meshCached = meshContainerFBX;
		meshCache = new MeshFilter[meshCacheCount];
		for (int i = 0; i < meshCacheCount; i++)
		{
			meshCache[i] = (MeshFilter)meshContainerFBX.GetChild(i).GetComponent(typeof(MeshFilter));
		}
		currentFrame = (float)meshCacheCount * UnityEngine.Random.value;
		meshFilter.sharedMesh = meshCache[(int)currentFrame].sharedMesh;
	}

	public virtual void CheckIfMeshHasChanged()
	{
		if (meshCached != meshContainerFBX)
		{
			if (meshContainerFBX != null)
			{
			}
			FillCacheArray();
		}
	}

	public virtual void AnimatedMesh()
	{
		delta = updateInterval;
		startDelayCounter += updateInterval;
		if (!(startDelayCounter <= startDelay))
		{
			rendererComponent.enabled = true;
			Animate();
		}
		if (enabled)
		{
			Invoke("AnimatedMesh", updateInterval);
		}
		else
		{
			rendererComponent.enabled = false;
		}
	}

	public virtual bool PingPongFrame()
	{
		if (pingPongToggle)
		{
			currentFrame += currentSpeed * delta;
		}
		else
		{
			currentFrame -= currentSpeed * delta;
		}
		int result;
		if (!(currentFrame > 0f))
		{
			currentFrame = 0f;
			pingPongToggle = true;
			result = 1;
		}
		else if (!(currentFrame < (float)meshCacheCount))
		{
			pingPongToggle = false;
			currentFrame = meshCacheCount - 1;
			result = 1;
		}
		else
		{
			result = 0;
		}
		return (byte)result != 0;
	}

	public virtual bool NextFrame()
	{
		currentFrame += currentSpeed * delta;
		int result;
		if (!(currentFrame <= (float)(meshCacheCount + 1)))
		{
			currentFrame = 0f;
			if (!loop)
			{
				enabled = false;
			}
			result = 1;
		}
		else if (!(currentFrame < (float)meshCacheCount))
		{
			currentFrame = (float)meshCacheCount - currentFrame;
			if (!loop)
			{
				enabled = false;
			}
			result = 1;
		}
		else
		{
			result = 0;
		}
		return (byte)result != 0;
	}

	public virtual void RandomizePropertiesAfterLoop()
	{
		if (randomSpeedLoop)
		{
			RandomizePlaySpeed();
		}
		if (randomRotateLoop)
		{
			RandomRotate();
		}
	}

	public virtual void Animate()
	{
		if (rendererComponent.isVisible)
		{
			if (pingPong && PingPongFrame())
			{
				RandomizePropertiesAfterLoop();
			}
			else if (!pingPong && NextFrame())
			{
				RandomizePropertiesAfterLoop();
			}
			meshFilter.sharedMesh = meshCache[(int)currentFrame].sharedMesh;
		}
	}

	public virtual void Main()
	{
	}
}
