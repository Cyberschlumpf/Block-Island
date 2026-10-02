Shader "VoxelEngine/Diffuse" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	
	SubShader {
		Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
		LOD 200
		
		CGPROGRAM
      	#pragma surface surf Lambert noambient addshadow fullforwardshadows
      	#include "Lighting.inc"
      	
      	struct Input {
        	float2 uv_MainTex;
        	float4 color : COLOR;
      	};
      	sampler2D _MainTex;
		
      	void surf(Input IN, inout SurfaceOutput o) {
      		float3 color = tex2D (_MainTex, IN.uv_MainTex).rgb;
      		o.Albedo = color;
        	o.Emission = Lighting(color, IN.color);
     	}
		
		ENDCG
	}
	
	FallBack "Unlit/Texture"
}
