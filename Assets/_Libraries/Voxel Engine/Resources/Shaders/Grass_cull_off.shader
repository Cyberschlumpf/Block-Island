Shader "VoxelEngine/Grass_cull_off" {
	Properties {
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}
	
	SubShader {
		Tags { "Queue" = "AlphaTest" "RenderType" = "Transparent" }
		Cull Off
		LOD 200
		
		CGPROGRAM
      	#pragma surface surf Lambert noambient addshadow fullforwardshadows vertex:vert
      	#include "Lighting.inc"
      	
		struct Input {
        	float2 uv_MainTex;
        	float4 color : COLOR;
      	};
		sampler2D _MainTex;
		
		void vert (inout appdata_full v) {
			float k = frac(v.vertex.y) * 0.16;
            float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float gust = sin(_Time.y * 1.35 + wpos.x * 0.23 + wpos.z * 0.17);
            float cross = sin(_Time.y * 0.78 + wpos.z * 0.31) * 0.45;
			v.vertex.x += (gust + cross) * k;
            v.vertex.z += sin(_Time.y * 1.05 + wpos.x * 0.19) * k * 0.35;
      	}
		
      	void surf (Input IN, inout SurfaceOutput o) {
      		float4 color = tex2D (_MainTex, IN.uv_MainTex);
      		
      		clip(color.a-0.5f);
        	o.Albedo = color.rgb;
        	o.Alpha = color.a;
			o.Emission = Lighting(color, IN.color);
     	}
		
		ENDCG
	}
	
	FallBack "Unlit/Texture"
}
