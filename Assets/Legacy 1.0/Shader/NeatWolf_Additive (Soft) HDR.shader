// Block Island 1.0 migration: rebuilt from the original material properties (source was a decompiler placeholder).
Shader "NeatWolf/Additive (Soft) HDR" {
	Properties {
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01, 3)) = 1
		_DiffuseBoost ("Diffuse Boost", Float) = 1
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Blend SrcAlpha One
		ZWrite Off
		Cull Off
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"
			sampler2D _MainTex;
			float _InvFade;
			float _DiffuseBoost;
			struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; float3 n : TEXCOORD1; float3 v : TEXCOORD2; UNITY_FOG_COORDS(3) };
			v2f vert (appdata_full v) {
				v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord.xy; o.color = v.color;
				o.n = UnityObjectToWorldNormal(v.normal); o.v = normalize(WorldSpaceViewDir(v.vertex));
				UNITY_TRANSFER_FOG(o, o.pos); return o;
			}
			fixed4 frag (v2f i) : SV_Target {
				fixed4 c = tex2D(_MainTex, i.uv) * i.color;
				UNITY_APPLY_FOG_COLOR(i.fogCoord, c, fixed4(0,0,0,0));
				return c;
			}
			ENDCG
		}
	}
}
