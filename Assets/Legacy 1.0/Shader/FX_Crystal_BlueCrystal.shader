// Block Island 1.0 migration: rebuilt from the original material properties (source was a decompiler placeholder).
Shader "FX/Crystal/BlueCrystal" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_ReflectTex ("Reflection Texture", Cube) = "" {}
		_RefractTex ("Refraction Texture", Cube) = "" {}
	}
	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 200
		Cull Back
		CGPROGRAM
		#pragma surface surf Lambert alpha:fade
		#pragma target 3.0
		fixed4 _Color;
		samplerCUBE _ReflectTex;
		samplerCUBE _RefractTex;
		struct Input { float2 uv_MainTex; float3 viewDir; float3 worldRefl; float4 color : COLOR; INTERNAL_DATA };
		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 r = texCUBE(_ReflectTex, WorldReflectionVector(IN, o.Normal));
			o.Albedo = _Color.rgb * 0.2;
			o.Emission = r.rgb * _Color.rgb;
			o.Alpha = 0.6 * _Color.a;
		}
		ENDCG
	}
	FallBack "Legacy Shaders/Diffuse"
}
