// Block Island 1.0 migration: rebuilt from the original material properties (source was a decompiler placeholder).
Shader "Dark Anvil/Rim" {
	Properties {
		_MainTex ("Diffuse(RGB) Spec(A)", 2D) = "white" {}
		_BumpMap ("Bumpmap", 2D) = "bump" {}
		_RimColor ("Rim Color", Vector) = (0.26,0.19,0.16,0)
		_RimPower ("Rim Power", Range(0.5, 8)) = 3
		_SpecColor ("Specular Color", Vector) = (0.5,0.5,0.5,1)
		_Shininess ("Shininess", Range(0.01, 1)) = 0.078125
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Cull Back
		CGPROGRAM
		#pragma surface surf BlinnPhong fullforwardshadows
		#pragma target 3.0
		sampler2D _MainTex;
		sampler2D _BumpMap;
		fixed4 _RimColor;
		float _RimPower;
		float _Shininess;
		struct Input { float2 uv_MainTex; float3 viewDir; float3 worldRefl; float4 color : COLOR; INTERNAL_DATA };
		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
			o.Gloss = c.a; o.Specular = _Shininess;
			o.Emission = _RimColor.rgb * pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), _RimPower);
		}
		ENDCG
	}
	FallBack "Legacy Shaders/Diffuse"
}
