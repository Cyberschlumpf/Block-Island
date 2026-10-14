// Block Island 1.0 migration: rebuilt from the original material properties (source was a decompiler placeholder).
Shader "shader/2SideBumpMap" {
	Properties {
		_Color ("Main Color", Vector) = (1,1,1,1)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_BumpMap ("Normalmap", 2D) = "bump" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Cull Off
		CGPROGRAM
		#pragma surface surf Lambert fullforwardshadows
		#pragma target 3.0
		fixed4 _Color;
		sampler2D _MainTex;
		sampler2D _BumpMap;
		struct Input { float2 uv_MainTex; float3 viewDir; float3 worldRefl; float4 color : COLOR; INTERNAL_DATA };
		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
		}
		ENDCG
	}
	FallBack "Legacy Shaders/Diffuse"
}
