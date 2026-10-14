// Block Island 1.0 migration: rebuilt from the original material properties (source was a decompiler placeholder).
Shader "Custom/DisplayNightEarthTexture" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_SpotAngle ("Spot Angle", Float) = 30
		_Range ("Range", Float) = 5
		_Contrast ("Contrast", Range(20, 80)) = 50
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Cull Back
		CGPROGRAM
		#pragma surface surf Lambert fullforwardshadows
		#pragma target 3.0
		sampler2D _MainTex;
		float _SpotAngle;
		float _Range;
		float _Contrast;
		struct Input { float2 uv_MainTex; float3 viewDir; float3 worldRefl; float4 color : COLOR; INTERNAL_DATA };
		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
		}
		ENDCG
	}
	FallBack "Legacy Shaders/Diffuse"
}
