// Visible light beam of the beacon: additive, fades along its length (vertex alpha) and towards its edges.
Shader "Block Island/Light Beam" {
    Properties { _Color ("Color", Color) = (1, .95, .8, 1) _Intensity ("Intensity", Range(0, 2)) = .35 }
    SubShader {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha One ZWrite Off Cull Off Lighting Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color; float _Intensity;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float rim : TEXCOORD0; };
            v2f vert(appdata v) {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color;
                float3 n = normalize(UnityObjectToWorldNormal(v.normal)); float3 view = normalize(WorldSpaceViewDir(v.vertex));
                o.rim = saturate(abs(dot(n, view))); return o;
            }
            fixed4 frag(v2f i) : SV_Target { fixed4 c = _Color; c.a = i.color.a * i.rim * i.rim * _Intensity; return c; }
            ENDCG
        }
    }
}
