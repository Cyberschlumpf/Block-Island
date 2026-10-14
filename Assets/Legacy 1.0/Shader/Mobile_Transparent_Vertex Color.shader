// Block Island 1.0 migration: rebuilt from the original material properties (source was a decompiler placeholder).
Shader "Mobile/Transparent/Vertex Color" {
	Properties {
		_Color ("Main Color", Vector) = (1,1,1,1)
		_SpecColor ("Spec Color", Vector) = (1,1,1,0)
		_Emission ("Emmisive Color", Vector) = (0,0,0,0)
		_Shininess ("Shininess", Range(0.1, 1)) = 0.7
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		LOD 200
		Cull Back
		CGPROGRAM
		#pragma surface surf BlinnPhong alpha:fade
		#pragma target 3.0
		fixed4 _Color;
		fixed4 _Emission;
		float _Shininess;
		sampler2D _MainTex;
		struct Input { float2 uv_MainTex; float3 viewDir; float3 worldRefl; float4 color : COLOR; INTERNAL_DATA };
		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color * IN.color;
			o.Albedo = c.rgb;
			o.Gloss = c.a; o.Specular = _Shininess;
			o.Alpha = c.a;
			o.Emission = _Emission.rgb;
		}
		ENDCG
	}
	FallBack "Legacy Shaders/Diffuse"
}
