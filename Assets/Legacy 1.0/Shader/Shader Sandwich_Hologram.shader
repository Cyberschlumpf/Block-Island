// Block Island 1.0 migration: rebuilt from the original material properties (source was a decompiler placeholder).
Shader "Shader Sandwich/Hologram" {
	Properties {
		_SSSFlicker_Axis ("Flicker Axis", Vector) = (0.2794118,0,0.2794118,1)
		_SSSForce_Flicker ("Force Flicker", Range(0, 1)) = 0
		_SSSPull_Height ("Pull Height", Float) = 0.53
		_SSSPull_Axis ("Pull Axis", Vector) = (0,1,0,1)
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
			fixed4 _SSSFlicker_Axis;
			float _SSSForce_Flicker;
			float _SSSPull_Height;
			fixed4 _SSSPull_Axis;
			struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; float3 n : TEXCOORD1; float3 v : TEXCOORD2; UNITY_FOG_COORDS(3) };
			v2f vert (appdata_full v) {
				v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord.xy; o.color = v.color;
				o.n = UnityObjectToWorldNormal(v.normal); o.v = normalize(WorldSpaceViewDir(v.vertex));
				UNITY_TRANSFER_FOG(o, o.pos); return o;
			}
			fixed4 frag (v2f i) : SV_Target {
				fixed4 c = fixed4(0.3,0.8,1,1) * i.color;
				c.a *= pow(1.0 - saturate(abs(dot(normalize(i.n), i.v))), 1.5);
				UNITY_APPLY_FOG_COLOR(i.fogCoord, c, fixed4(0,0,0,0));
				return c;
			}
			ENDCG
		}
	}
}
