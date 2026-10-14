using UnityEngine;
using System.Collections;

public class FastPerlinNoise2D {

	private const int SIZE = 256;
	private const int SIZE_MASK = 256 - 1;
	private static readonly float[] random;
	private static readonly int[] perm;
	
	public readonly int octaves = 5;

	public float scale = 1;
	public Vector2 offset = Vector2.zero;

	static FastPerlinNoise2D() {
		random = new float[SIZE];
		perm = new int[SIZE*2];
		for (int i = 0 ; i < SIZE; i++) {
			random[i] = Random.Range(-0.5f, 0.5f);
			perm[i] = i;
		}

		for (int i = 0 ; i < SIZE; i++) {
			int rndIndex = Random.Range(0, SIZE);
			int tmp = perm[ rndIndex ];
			perm[ rndIndex ] = perm[ i ];
			perm[ i ] = tmp;
			perm[ i + SIZE ] = perm[ i ];
		}
	}

	public FastPerlinNoise2D(float scale, int octaves) {
		this.scale = scale;
		this.octaves = octaves;
	}
	
	public float Noise(float x, float y) {
		x *= scale;
		y *= scale;
		x += offset.x;
		y += offset.y;

		int freq = 1;
		float amp = 1;
		float total = 0;
		for(int i = 0; i < octaves; i++) {
			total += InterpolatedNoise(x*freq, y*freq) * amp;
			freq *= 2;
			amp *= 0.5f;
		}
		return total;
	}

	private static float InterpolatedNoise(float x, float y) {
		int xi = Floor( x );
		int yi = Floor( y );
		float fx = x - xi;
		float fy = y - yi;
		
		int x0 =   xi       & SIZE_MASK;
		int x1 = ( x0 + 1 ) & SIZE_MASK;
		int y0 =   yi       & SIZE_MASK;
		int y1 = ( y0 + 1 ) & SIZE_MASK;


		float v1 = random[ perm[ perm[ x0 ] + y0 ] ];
		float v2 = random[ perm[ perm[ x1 ] + y0 ] ];
		float v3 = random[ perm[ perm[ x0 ] + y1 ] ];
		float v4 = random[ perm[ perm[ x1 ] + y1 ] ];

		float tx = SmoothStep( fx );
		float ty = SmoothStep( fy );

		float v5 = Lerp( v1, v2, tx );
		float v6 = Lerp( v3, v4, tx );
		return     Lerp( v5, v6, ty );
	}

	private static int Floor( float x ) {
		return x > 0 ? (int) x : (int) x - 1; 
	}

	private static float Lerp( float a, float b, float t ) {
		return a + (b - a) * t;
	}

	private static float SmoothStep( float t ) {
		return t * t * ( 3 - 2 * t );
	}


}
