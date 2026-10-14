// Block Island 1.0 migration: rebuilt from the original material properties (source was a decompiler placeholder).
Shader "Dark Anvil/Diffuse DoubleFace" {
	Properties {
		_Color ("Main Color", Vector) = (1,1,1,1)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
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
		struct Input { float2 uv_MainTex; float3 viewDir; float3 worldRefl; float4 color : COLOR; INTERNAL_DATA };
		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Legacy Shaders/Diffuse"
}
