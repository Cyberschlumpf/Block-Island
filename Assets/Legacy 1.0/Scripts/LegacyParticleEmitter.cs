using UnityEngine;

// Block Island 1.0 migration: replaces the Unity 4 particle trio (EllipsoidParticleEmitter +
// ParticleAnimator + ParticleRenderer), which Unity 6 can no longer load. The original values are
// copied over by Migration 1.0 nach 2.0/partikel_ersatz.py; this component rebuilds the effect with a
// modern ParticleSystem. Emission is done here, like the legacy emitter, so start velocity and
// spawn volume match the original exactly; force, damping, colour and size growth use PS modules.
public class LegacyParticleEmitter : MonoBehaviour {

	// EllipsoidParticleEmitter
	public bool emit = true;
	public float minSize = .1f, maxSize = .1f;
	public float minEnergy = 3f, maxEnergy = 3f;
	public float minEmission = 50f, maxEmission = 50f;
	public Vector3 worldVelocity, localVelocity, rndVelocity, tangentVelocity;
	public float emitterVelocityScale = .05f;
	public float angularVelocity, rndAngularVelocity;
	public bool rndRotation;
	public bool useWorldSpace = true;
	public bool oneShot;
	public Vector3 ellipsoid = Vector3.one;
	public float minEmitterRange;

	// ParticleAnimator
	public bool animateColor = true;
	public Color[] colorAnimation = { Color.white, Color.white, Color.white, Color.white, Color.white };
	public float sizeGrow;
	public Vector3 force, rndForce;
	public float damping = 1f;

	// ParticleRenderer
	public Material material;
	public int renderMode;            // legacy: 0 Billboard, 2 Sorted, 3 Stretch, 4 Horizontal, 5 Vertical
	public float lengthScale = 2f, velocityScale, cameraVelocityScale;
	public float maxParticleSize = .25f;
	public int uvTilesX = 1, uvTilesY = 1, uvCycles = 1;

	ParticleSystem ps;
	float pending;
	Vector3 lastPos;

	void OnEnable() {
		ps = GetComponent<ParticleSystem>();
		if(ps == null) ps = gameObject.AddComponent<ParticleSystem>();
		Configure();
		lastPos = transform.position;
		pending = 0f;
		if(oneShot && emit) Emit(Mathf.RoundToInt(Random.Range(minEmission, maxEmission)), Vector3.zero);
	}

	void Configure() {
		ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
		var main = ps.main;
		main.playOnAwake = true;
		main.loop = true;
		main.duration = 1f;
		main.maxParticles = Mathf.Max(100, Mathf.CeilToInt(maxEmission * maxEnergy * 1.25f) + 16);
		main.simulationSpace = useWorldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
		main.startSpeed = 0f;
		main.startLifetime = new ParticleSystem.MinMaxCurve(minEnergy, maxEnergy);
		main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
		main.scalingMode = ParticleSystemScalingMode.Hierarchy;

		var emission = ps.emission; emission.enabled = false;   // emitted from Update(), see class comment
		var shape = ps.shape; shape.enabled = false;

		var col = ps.colorOverLifetime; col.enabled = animateColor && colorAnimation != null && colorAnimation.Length > 0;
		if(col.enabled) {
			int n = Mathf.Min(colorAnimation.Length, 8);
			var ck = new GradientColorKey[n]; var ak = new GradientAlphaKey[n];
			for(int i = 0; i < n; i++) {
				float t = n == 1 ? 0f : i / (float)(n - 1);
				ck[i] = new GradientColorKey(colorAnimation[i], t);
				ak[i] = new GradientAlphaKey(colorAnimation[i].a, t);
			}
			Gradient g = new Gradient(); g.SetKeys(ck, ak);
			col.color = new ParticleSystem.MinMaxGradient(g);
		}

		var force3 = ps.forceOverLifetime;
		force3.enabled = force != Vector3.zero || rndForce != Vector3.zero;
		if(force3.enabled) {
			force3.space = ParticleSystemSimulationSpace.World;
			force3.x = new ParticleSystem.MinMaxCurve(force.x - rndForce.x, force.x + rndForce.x);
			force3.y = new ParticleSystem.MinMaxCurve(force.y - rndForce.y, force.y + rndForce.y);
			force3.z = new ParticleSystem.MinMaxCurve(force.z - rndForce.z, force.z + rndForce.z);
			force3.randomized = rndForce != Vector3.zero;
		}

		// Legacy damping multiplies velocity by `damping` per second (1 = none).
		var limit = ps.limitVelocityOverLifetime;
		limit.enabled = damping < .999f;
		if(limit.enabled) {
			limit.limit = 0f;
			limit.dampen = Mathf.Clamp01(1f - Mathf.Pow(Mathf.Clamp(damping, 0.0001f, 1f), 1f / 60f));
		}

		var size = ps.sizeOverLifetime;
		size.enabled = Mathf.Abs(sizeGrow) > .0001f;
		if(size.enabled) {
			// Legacy: size += size * sizeGrow * dt  ->  exponential growth over the particle's life.
			float life = (minEnergy + maxEnergy) * .5f;
			float end = Mathf.Max(.01f, Mathf.Exp(sizeGrow * life));
			float peak = Mathf.Max(1f, end);
			AnimationCurve curve = new AnimationCurve();
			for(int i = 0; i <= 8; i++) { float t = i / 8f; curve.AddKey(t, Mathf.Exp(sizeGrow * life * t) / peak); }
			size.size = new ParticleSystem.MinMaxCurve(peak, curve);
		}

		var rot = ps.rotationOverLifetime;
		rot.enabled = Mathf.Abs(rndAngularVelocity) > .0001f || Mathf.Abs(angularVelocity) > .0001f;
		if(rot.enabled) rot.z = new ParticleSystem.MinMaxCurve((angularVelocity - rndAngularVelocity) * Mathf.Deg2Rad, (angularVelocity + rndAngularVelocity) * Mathf.Deg2Rad);

		var sheet = ps.textureSheetAnimation;
		sheet.enabled = uvTilesX * uvTilesY > 1;
		if(sheet.enabled) { sheet.numTilesX = uvTilesX; sheet.numTilesY = uvTilesY; sheet.cycleCount = Mathf.Max(1, uvCycles); }

		var r = GetComponent<ParticleSystemRenderer>();
		r.sharedMaterial = material;
		r.maxParticleSize = maxParticleSize;
		switch(renderMode) {
			case 3: r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = lengthScale; r.velocityScale = velocityScale; r.cameraVelocityScale = cameraVelocityScale; break;
			case 4: r.renderMode = ParticleSystemRenderMode.HorizontalBillboard; break;
			case 5: r.renderMode = ParticleSystemRenderMode.VerticalBillboard; break;
			default: r.renderMode = ParticleSystemRenderMode.Billboard; break;
		}
		ps.Play();
	}

	void Update() {
		if(ps == null) return;
		float dt = Time.deltaTime;
		Vector3 emitterVel = dt > 0f ? (transform.position - lastPos) / dt : Vector3.zero;
		lastPos = transform.position;
		if(!emit || oneShot || dt <= 0f) return;
		pending += Random.Range(minEmission, maxEmission) * dt;
		int n = (int)pending;
		if(n > 0) { pending -= n; Emit(n, emitterVel); }
	}

	void Emit(int count, Vector3 emitterVel) {
		var p = new ParticleSystem.EmitParams();
		for(int i = 0; i < count; i++) {
			Vector3 dir = Random.onUnitSphere;
			float radius = Mathf.Lerp(minEmitterRange, 1f, Mathf.Pow(Random.value, 1f / 3f));
			Vector3 local = Vector3.Scale(dir * radius * .5f, ellipsoid);
			Vector3 tangent = Vector3.Scale(Vector3.Cross(dir, Vector3.up).normalized, tangentVelocity);
			Vector3 v = worldVelocity + transform.TransformDirection(localVelocity + tangent)
				+ new Vector3(Random.Range(-1f, 1f) * rndVelocity.x, Random.Range(-1f, 1f) * rndVelocity.y, Random.Range(-1f, 1f) * rndVelocity.z)
				+ emitterVel * emitterVelocityScale;
			if(useWorldSpace) { p.position = transform.TransformPoint(local); p.velocity = v; }
			else { p.position = local; p.velocity = transform.InverseTransformDirection(v); }
			p.startLifetime = Random.Range(minEnergy, maxEnergy);
			p.startSize = Random.Range(minSize, maxSize);
			p.rotation = rndRotation ? Random.Range(0f, 360f) : 0f;
			p.applyShapeToPosition = false;
			ps.Emit(p, 1);
		}
	}
}
