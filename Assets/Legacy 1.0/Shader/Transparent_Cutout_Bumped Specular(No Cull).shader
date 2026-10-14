// Block Island 1.0 migration: rebuilt from the original material properties (source was a decompiler placeholder).
Shader "Transparent/Cutout/Bumped Specular(No Cull)" {
	Properties {
		_Color ("Main Color", Vector) = (1,1,1,1)
		_SpecColor ("Specular Color", Vector) = (0.5,0.5,0.5,0)
		_Shininess ("Shininess", Range(0.01, 1)) = 0.078125
		_MainTex ("Base (RGB) TransGloss (A)", 2D) = "white" {}
		_BumpMap ("Normalmap", 2D) = "bump" {}
		_Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
	}
	SubShader {
		Tags { "Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout" }
		LOD 200
		Cull Off
		CGPROGRAM
		#pragma surface surf BlinnPhong fullforwardshadows alphatest:_Cutoff addshadow
		#pragma target 3.0
		fixed4 _Color;
		float _Shininess;
		sampler2D _MainTex;
		sampler2D _BumpMap;
		struct Input { float2 uv_MainTex; float3 viewDir; float3 worldRefl; float4 color : COLOR; INTERNAL_DATA };
		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
			o.Gloss = c.a; o.Specular = _Shininess;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Legacy Shaders/Diffuse"
}
