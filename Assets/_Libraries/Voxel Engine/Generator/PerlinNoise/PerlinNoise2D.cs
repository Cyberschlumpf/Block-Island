using UnityEngine;
using System.Collections;

public class PerlinNoise2D {
	
	public float scale;
	public Vector2 offset = Vector2.zero;
	private float persistence = 0.5f;
	private int octaves = 5;
	
	public PerlinNoise2D(float scale) {
		this.scale = scale;
		SetRandomOffset();
	}

	public void SetRandomOffset() {
		offset = new Vector2( Random.Range(-100f, 100f), Random.Range(-100f, 100f) );
	}
	
	public PerlinNoise2D SetPersistence(float persistence) {
		this.persistence = persistence;
		return this;
	}
	
	public PerlinNoise2D SetOctaves(int octaves) {
		this.octaves = octaves;
		return this;
	}
	
	public float Noise(float x, float y) {
		x *= scale;
		y *= scale;
		x += offset.x;
		y += offset.y;
	
		float frequency = 1;
		float amplitude = 1;

		float sum = 0;
		for(int i=0; i<octaves; i++) {
			if(i >= 1) {
				frequency *= 2;
				amplitude *= persistence;
			}
			sum += InterpolatedNoise(x * frequency, y * frequency) * amplitude;
		}
		return sum;
	}



	private static float InterpolatedNoise(float x, float y) {
		int ix = Mathf.FloorToInt( x );
		int iy = Mathf.FloorToInt( y );
		float fx = x - ix;
		float fy = y - iy;
		
		float v1 = SmoothNoise(ix,     iy);
		float v2 = SmoothNoise(ix + 1, iy);
		float v3 = SmoothNoise(ix,     iy + 1);
		float v4 = SmoothNoise(ix + 1, iy + 1);
		
		float i1 = Mathf.Lerp(v1, v2, fx);
		float i2 = Mathf.Lerp(v3, v4, fx);
		return Mathf.Lerp(i1, i2, fy);
	}
	
	/*private static float CosLerp(float a, float b, float t) {
		float cos = Mathf.Cos(t * Mathf.PI);
		t = (1 - cos) * 0.5f;
		return Mathf.Lerp( a, b, t );
	}
	
	private static float CubicLerp(float v0, float v1, float v2, float v3, float t) {
		float P = (v3 - v2) - (v0 - v1);
		float Q = (v0 - v1) - P;
		float R = v2 - v0;
		float S = v1;
		return P*t*3 + Q*t*2 + R*t + S;
	}*/
	
	private static float SmoothNoise(int x, int y) {
		float corners = ( Noise(x-1, y-1)+Noise(x+1, y-1)+Noise(x-1, y+1)+Noise(x+1, y+1) ) / 16;
		float sides   = ( Noise(x-1, y)  +Noise(x+1, y)  +Noise(x, y-1)  +Noise(x, y+1) ) /  8;
		float center  =  Noise(x, y) / 4;
		return corners + sides + center;
	}
	
	private static float Noise(int x, int y) { // [-1.0 ... 1.0]
		int n = x + y * 57;
		n = (n<<13) ^ n;
		return 1f - ( (n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f;    
	}
	
}
