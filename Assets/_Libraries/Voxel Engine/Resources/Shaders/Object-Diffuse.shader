Shader "VoxelEngine/Object-Diffuse" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Light("Light", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert noambient addshadow fullforwardshadows
		#include "Lighting.inc"

		sampler2D _MainTex;
		float4 _Light;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb;
        	o.Emission = Lighting(o.Albedo, _Light);
		}
		ENDCG
	} 
	FallBack "Unlit/Texture"
}
