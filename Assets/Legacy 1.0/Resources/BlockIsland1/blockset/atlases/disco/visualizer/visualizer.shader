// Block Island 1.0 migration: rebuilt from the original material properties (source was a decompiler placeholder).
Shader "Custom/Visualizer" {
	Properties {
		[HideInInspector] _MainTex ("Base (RGB)", 2D) = "white" {}
		_ReflectionTex ("Base (RGB)", 2D) = "white" {}
		_Spectra ("Spectra", Vector) = (0,0,0,0)
		_Center ("Center", Vector) = (0,0,0,1)
		_RingSrtide ("Stride", Float) = 0.2
		_RingThicknessMin ("ThicknessMin", Float) = 0.1
		_RingThicknessMax ("ThicknessMax", Float) = 0.5
		_RingEmission ("RingEmission", Float) = 10
		_RingSpeedMin ("RingSpeedMin", Float) = 0.2
		_RingSpeedMax ("RingSpeedMin", Float) = 0.5
		_GridColor ("GridColor", Vector) = (0.2,0.3,0.5,1)
		_GridEmission ("GridEmission", Float) = 8
		_ReflectionStrength ("ReflectionStrength", Float) = 0.2
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		Cull Off
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"
			sampler2D _ReflectionTex;
			fixed4 _Spectra;
			fixed4 _Center;
			float _RingSrtide;
			float _RingThicknessMin;
			float _RingThicknessMax;
			float _RingEmission;
			float _RingSpeedMin;
			float _RingSpeedMax;
			fixed4 _GridColor;
			float _GridEmission;
			float _ReflectionStrength;
			struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; float3 n : TEXCOORD1; float3 v : TEXCOORD2; UNITY_FOG_COORDS(3) };
			v2f vert (appdata_full v) {
				v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord.xy; o.color = v.color;
				o.n = UnityObjectToWorldNormal(v.normal); o.v = normalize(WorldSpaceViewDir(v.vertex));
				UNITY_TRANSFER_FOG(o, o.pos); return o;
			}
			fixed4 frag (v2f i) : SV_Target {
				fixed4 c = tex2D(_ReflectionTex, i.uv) * i.color;
				c.a *= 1.0;
				UNITY_APPLY_FOG(i.fogCoord, c);
				return c;
			}
			ENDCG
		}
	}
}
