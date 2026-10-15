// Lying snow on blocks: soft white flakes lying flat on a surface. The depth offset draws them on top of the
// block face they lie on (no flickering or vanishing into the ground), but walls and roofs in front still hide them.
Shader "Block Island/Lying Snow" {
    Properties {
        _MainTex ("Flake", 2D) = "white" {}
    }
    SubShader {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off Lighting Off ZWrite Off
        Offset -2, -8
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; UNITY_FOG_COORDS(1) };
            v2f vert(appdata v) {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color; o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.pos); return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                fixed4 t = tex2D(_MainTex, i.uv);
                fixed a = saturate(max(t.a * max(t.r, max(t.g, t.b)), 0) * 1.6) * i.color.a;   // shape from the flake texture
                fixed4 c = fixed4(0.94, 0.95, 0.98, a);                                        // snow white, not grey
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
}
