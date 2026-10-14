using System;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace MirzaBeig.VFX
{
	[Serializable]
	public class ParticleSystems : MonoBehaviour
	{
		public delegate void onParticleSystemsDeadEventHandler();

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private onParticleSystemsDeadEventHandler onParticleSystemsDeadEvent__BackingField;

		public ParticleSystem[] particleSystems { get; set; }

		public event onParticleSystemsDeadEventHandler onParticleSystemsDeadEvent
		{
			add
			{
				onParticleSystemsDeadEventHandler onParticleSystemsDeadEventHandler2 = onParticleSystemsDeadEvent__BackingField;
				onParticleSystemsDeadEventHandler onParticleSystemsDeadEventHandler3;
				do
				{
					onParticleSystemsDeadEventHandler3 = onParticleSystemsDeadEventHandler2;
					onParticleSystemsDeadEventHandler2 = Interlocked.CompareExchange(ref onParticleSystemsDeadEvent__BackingField, (onParticleSystemsDeadEventHandler)Delegate.Combine(onParticleSystemsDeadEventHandler3, value), onParticleSystemsDeadEventHandler2);
				}
				while ((object)onParticleSystemsDeadEventHandler2 != onParticleSystemsDeadEventHandler3);
			}
			remove
			{
				onParticleSystemsDeadEventHandler onParticleSystemsDeadEventHandler2 = onParticleSystemsDeadEvent__BackingField;
				onParticleSystemsDeadEventHandler onParticleSystemsDeadEventHandler3;
				do
				{
					onParticleSystemsDeadEventHandler3 = onParticleSystemsDeadEventHandler2;
					onParticleSystemsDeadEventHandler2 = Interlocked.CompareExchange(ref onParticleSystemsDeadEvent__BackingField, (onParticleSystemsDeadEventHandler)Delegate.Remove(onParticleSystemsDeadEventHandler3, value), onParticleSystemsDeadEventHandler2);
				}
				while ((object)onParticleSystemsDeadEventHandler2 != onParticleSystemsDeadEventHandler3);
			}
		}

		protected virtual void Awake()
		{
			particleSystems = GetComponentsInChildren<ParticleSystem>();
		}

		protected virtual void Start()
		{
		}

		protected virtual void Update()
		{
		}

		protected virtual void LateUpdate()
		{
			if (!isAlive() && onParticleSystemsDeadEvent__BackingField != null)
			{
				onParticleSystemsDeadEvent__BackingField();
			}
		}

		public void play()
		{
			for (int i = 0; i < particleSystems.Length; i++)
			{
				particleSystems[i].Play();
			}
		}

		public void pause()
		{
			for (int i = 0; i < particleSystems.Length; i++)
			{
				particleSystems[i].Pause();
			}
		}

		public void stop()
		{
			for (int i = 0; i < particleSystems.Length; i++)
			{
				particleSystems[i].Stop();
			}
		}

		public void clear()
		{
			for (int i = 0; i < particleSystems.Length; i++)
			{
				particleSystems[i].Clear();
			}
		}

		public void setLoop(bool loop)
		{
			for (int i = 0; i < particleSystems.Length; i++)
			{
				particleSystems[i].loop = loop;
			}
		}

		public void setPlaybackSpeed(float speed)
		{
			for (int i = 0; i < particleSystems.Length; i++)
			{
				particleSystems[i].playbackSpeed = speed;
			}
		}

		public bool isAlive()
		{
			for (int i = 0; i < particleSystems.Length; i++)
			{
				if ((bool)particleSystems[i] && particleSystems[i].IsAlive())
				{
					return true;
				}
			}
			return false;
		}

		public bool isPlaying(bool checkAll = false)
		{
			if (particleSystems.Length == 0)
			{
				return false;
			}
			if (!checkAll)
			{
				return particleSystems[0].isPlaying;
			}
			for (int i = 0; i < 0; i++)
			{
				if (!particleSystems[i].isPlaying)
				{
					return false;
				}
			}
			return true;
		}

		public int getParticleCount()
		{
			int num = 0;
			for (int i = 0; i < particleSystems.Length; i++)
			{
				if ((bool)particleSystems[i])
				{
					num += particleSystems[i].particleCount;
				}
			}
			return num;
		}
	}
}
