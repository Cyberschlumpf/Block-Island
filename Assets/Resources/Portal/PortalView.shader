// Portal surface: a window into the other world. Shows the 360° picture taken there (a cubemap), looked up by the
// viewing direction, so the view shifts like through a real window when the player moves. A soft shimmer moves
// over it and the edges of the cube glow in the portal colour.
Shader "Block Island/Portal View" {
    Properties {
        _Cube ("Other world (360°)", CUBE) = "" {}
        _EdgeColor ("Edge glow", Color) = (0.45, 0.75, 1, 1)
        _Shimmer ("Shimmer", Range(0, 0.1)) = 0.025
        _Brightness ("Brightness", Range(0.2, 2)) = 1
    }
    SubShader {
        Tags { "Queue"="Geometry+10" "RenderType"="Opaque" }
        Cull Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            samplerCUBE _Cube;
            fixed4 _EdgeColor;
            float _Shimmer, _Brightness;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 world : TEXCOORD0; float3 local : TEXCOORD1; UNITY_FOG_COORDS(2) };
            v2f vert(appdata v) {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz; o.local = v.vertex.xyz;
                UNITY_TRANSFER_FOG(o, o.pos); return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                float t = _Time.y;
                float3 dir = normalize(i.world - _WorldSpaceCameraPos);
                // shimmer: the picture wobbles gently like a surface of light
                dir += _Shimmer * float3(sin(i.world.y * 3.1 + t * 1.7), sin(i.world.x * 2.7 + t * 1.3), sin(i.world.z * 2.9 + t * 1.9));
                fixed3 view = texCUBE(_Cube, normalize(dir)).rgb * _Brightness;
                // edges of the cube: two of the three local coordinates are close to +-0.5
                float3 a = abs(i.local) * 2;
                float3 s = smoothstep(0.86, 1.0, a);
                float edge = max(max(s.x * s.y, s.y * s.z), s.x * s.z);
                float pulse = 0.75 + 0.25 * sin(t * 2.2);
                // slow bright waves wandering over the surface
                float wave = pow(saturate(sin(dot(i.world, float3(0.9, 1.3, 0.7)) * 1.6 - t * 2.0)), 12) * 0.18;
                fixed3 c = view + _EdgeColor.rgb * (edge * 1.4 * pulse + wave);
                fixed4 col = fixed4(c, 1);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
