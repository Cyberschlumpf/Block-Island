Shader "VoxelEngine/Alpha-Diffuse_cull_off" {
	Properties {
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}
	
	SubShader {
		Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" }
		LOD 200
		Cull off
		
		CGPROGRAM
      	#pragma surface surf Lambert noambient addshadow fullforwardshadows
      	#include "Lighting.inc"
      	
		struct Input {
        	float2 uv_MainTex;
        	float4 color : COLOR;
      	};
		sampler2D _MainTex;
		
      	void surf (Input IN, inout SurfaceOutput o) {
      		float4 color = tex2D (_MainTex, IN.uv_MainTex);
        	o.Albedo = color.rgb;
        	o.Alpha = color.a;
			o.Emission = Lighting(color, IN.color);
			clip(color.a-0.5f);
     	}
		
		ENDCG
	} 
	FallBack "Diffuse"
}
