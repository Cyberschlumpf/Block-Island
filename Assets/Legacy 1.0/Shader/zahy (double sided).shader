// Block Island 1.0 migration: rebuilt from the original material properties (source was a decompiler placeholder).
Shader "zahy (double sided)" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Albedo", 2D) = "white" {}
		_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		[Gamma] _Metallic ("Metallic", Range(0, 1)) = 0
		_MetallicGlossMap ("Metallic", 2D) = "white" {}
		_BumpScale ("Scale", Float) = 1
		_BumpMap ("Normal Map", 2D) = "bump" {}
		_Parallax ("Height Scale", Range(0.005, 0.08)) = 0.02
		_ParallaxMap ("Height Map", 2D) = "black" {}
		_OcclusionStrength ("Strength", Range(0, 1)) = 1
		_OcclusionMap ("Occlusion", 2D) = "white" {}
		_EmissionColor ("Color", Vector) = (0,0,0,1)
		_EmissionMap ("Emission", 2D) = "white" {}
		_DetailMask ("Detail Mask", 2D) = "white" {}
		_DetailAlbedoMap ("Detail Albedo x2", 2D) = "grey" {}
		_DetailNormalMapScale ("Scale", Float) = 1
		_DetailNormalMap ("Normal Map", 2D) = "bump" {}
		[Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0
		[HideInInspector] _Mode ("__mode", Float) = 0
		[HideInInspector] _SrcBlend ("__src", Float) = 1
		[HideInInspector] _DstBlend ("__dst", Float) = 0
		[HideInInspector] _ZWrite ("__zw", Float) = 1
	}
	SubShader {
		Tags { "Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout" }
		LOD 200
		Cull Off
		CGPROGRAM
		#pragma surface surf Lambert fullforwardshadows alphatest:_Cutoff addshadow
		#pragma target 3.0
		fixed4 _Color;
		sampler2D _MainTex;
		float _Glossiness;
		float _Metallic;
		sampler2D _MetallicGlossMap;
		float _BumpScale;
		sampler2D _BumpMap;
		float _Parallax;
		sampler2D _ParallaxMap;
		float _OcclusionStrength;
		sampler2D _OcclusionMap;
		fixed4 _EmissionColor;
		sampler2D _EmissionMap;
		sampler2D _DetailMask;
		sampler2D _DetailAlbedoMap;
		float _DetailNormalMapScale;
		sampler2D _DetailNormalMap;
		float _UVSec;
		float _Mode;
		float _SrcBlend;
		float _DstBlend;
		float _ZWrite;
		struct Input { float2 uv_MainTex; float3 viewDir; float3 worldRefl; float4 color : COLOR; INTERNAL_DATA };
		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Legacy Shaders/Diffuse"
}
