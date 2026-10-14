Shader "VoxelEngine/Water" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	
	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 200
		
		CGPROGRAM
      	#pragma surface surf Lambert noambient vertex:vert
      	#include "Lighting.inc"
      	
      	struct Input {
        	float2 uv_MainTex;
        	float4 color : COLOR;
      	};
      	sampler2D _MainTex;
        float _VoxelBoxWaterEffects;
        float _VoxelBoxWaterReflection;
      	
      	
      	void vert (inout appdata_full v) {
        	float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
        	float4 pos = v.vertex;
            float wave1 = sin(wpos.x * 0.72 + _Time.y * 1.25);
            float wave2 = sin(wpos.z * 0.58 + _Time.y * 0.92);
            float wave3 = sin((wpos.x + wpos.z) * 0.31 + _Time.y * 0.63);
        	// 1.15: keep a base world-water motion active at all times.
            // WaterEffects adds extra amplitude, but can no longer freeze the water completely.
            float baseWave = wave1 * 0.020 + wave2 * 0.016 + wave3 * 0.010;
            float extraWave = wave1 * 0.018 + wave2 * 0.014 + wave3 * 0.010;
            pos.y += baseWave + extraWave * saturate(_VoxelBoxWaterEffects) - 0.08;
        	v.vertex = pos;
      	}
		
      	void surf (Input IN, inout SurfaceOutput o) {
      		// 1.15: animate the actual in-game water texture as well as the mesh.
            // Two slow directions avoid the old static tiled-water appearance.
            float2 uv1 = IN.uv_MainTex + float2(_Time.y * 0.025, _Time.y * 0.012);
            float2 uv2 = IN.uv_MainTex + float2(-_Time.y * 0.014, _Time.y * 0.021);
            fixed3 waterA = tex2D(_MainTex, uv1).rgb;
            fixed3 waterB = tex2D(_MainTex, uv2).rgb;
            o.Albedo = lerp(waterA, waterB, 0.35);
      		o.Alpha = 0.5;
        	o.Emission = Lighting(o.Albedo, IN.color);
     	}
		
		ENDCG
	}
	
	FallBack "Unlit/Texture"
}
