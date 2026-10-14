// Block Island 1.0 migration: rebuilt from the original material properties (source was a decompiler placeholder).
Shader "AVProVideo/Lit/Diffuse (texture+color+fog+stereo support)" {
	Properties {
		_Color ("Main Color", Vector) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_ChromaTex ("Chroma", 2D) = "white" {}
		[KeywordEnum(None, Top_Bottom, Left_Right, Custom_UV)] Stereo ("Stereo Mode", Float) = 0
		[Toggle(STEREO_DEBUG)] _StereoDebug ("Stereo Debug Tinting", Float) = 0
		[Toggle(APPLY_GAMMA)] _ApplyGamma ("Apply Gamma", Float) = 0
		[Toggle(USE_YPCBCR)] _UseYpCbCr ("Use YpCbCr", Float) = 0
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
		sampler2D _ChromaTex;
		float Stereo;
		float _StereoDebug;
		float _ApplyGamma;
		float _UseYpCbCr;
		struct Input { float2 uv_MainTex; float3 viewDir; float3 worldRefl; float4 color : COLOR; INTERNAL_DATA };
		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
		}
		ENDCG
	}
	FallBack "Legacy Shaders/Diffuse"
}
