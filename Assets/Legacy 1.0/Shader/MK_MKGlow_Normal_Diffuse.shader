// Block Island 1.0 migration: rebuilt from the original material properties (source was a decompiler placeholder).
Shader "MK/MKGlow/Normal/Diffuse" {
	Properties {
		_Color ("Main Color", Vector) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_MKGlowColor ("Glow Color", Vector) = (1,1,1,1)
		_MKGlowPower ("Glow Power", Range(0, 2.5)) = 1
		_MKGlowTex ("Glow Texture", 2D) = "black" {}
		_MKGlowTexColor ("Glow Texture Color", Vector) = (1,1,1,1)
		_MKGlowTexStrength ("Glow Texture Strength ", Range(0, 10)) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Cull Back
		CGPROGRAM
		#pragma surface surf Lambert fullforwardshadows
		#pragma target 3.0
		fixed4 _Color;
		sampler2D _MainTex;
		fixed4 _MKGlowColor;
		float _MKGlowPower;
		sampler2D _MKGlowTex;
		fixed4 _MKGlowTexColor;
		float _MKGlowTexStrength;
		struct Input { float2 uv_MainTex; float3 viewDir; float3 worldRefl; float4 color : COLOR; INTERNAL_DATA };
		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Emission = tex2D(_MKGlowTex, IN.uv_MainTex).rgb * _MKGlowTexColor.rgb * _MKGlowColor.rgb * _MKGlowPower;
		}
		ENDCG
	}
	FallBack "Legacy Shaders/Diffuse"
}
