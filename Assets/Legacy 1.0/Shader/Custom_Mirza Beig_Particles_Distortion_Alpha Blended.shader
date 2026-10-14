// Block Island 1.0 migration: rebuilt from the original material properties (source was a decompiler placeholder).
Shader "Custom/Mirza Beig/Particles/Distortion/Alpha Blended" {
	Properties {
		_opacity ("Opacity", Range(0, 2)) = 0.5
		_MainTex ("Alpha Mask", 2D) = "white" {}
		_DistTex ("Distortion Texture", 2D) = "bump" {}
		_InvFade ("Soft Particles Factor", Range(0.01, 8)) = 1
		_distortion ("Distortion", Range(0, 1024)) = 10
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
			float _opacity;
			sampler2D _MainTex;
			sampler2D _DistTex;
			float _InvFade;
			float _distortion;
			struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; float3 n : TEXCOORD1; float3 v : TEXCOORD2; UNITY_FOG_COORDS(3) };
			v2f vert (appdata_full v) {
				v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord.xy; o.color = v.color;
				o.n = UnityObjectToWorldNormal(v.normal); o.v = normalize(WorldSpaceViewDir(v.vertex));
				UNITY_TRANSFER_FOG(o, o.pos); return o;
			}
			fixed4 frag (v2f i) : SV_Target {
				fixed4 c = tex2D(_MainTex, i.uv) * i.color;
				c.a *= 1.0 * _opacity;
				UNITY_APPLY_FOG(i.fogCoord, c);
				return c;
			}
			ENDCG
		}
	}
}
