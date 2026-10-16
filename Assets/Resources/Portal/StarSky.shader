// Calm star sky for the portal journey: a nebula cubemap (from Galaxy Materials) seen from inside a sphere,
// darkened and with soft contrast so it is easy on the eyes. _Fade fades it in and out.
Shader "Block Island/Star Sky" {
    Properties {
        _Cube ("Nebula", CUBE) = "" {}
        _Brightness ("Brightness", Range(0, 2)) = .55
        _Contrast ("Darken the background", Range(1, 3)) = 1.6
        _Fade ("Fade", Range(0, 1)) = 1
    }
    SubShader {
        Tags { "Queue"="Background" "RenderType"="Opaque" }
        Cull Front ZWrite Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            samplerCUBE _Cube;
            float _Brightness, _Contrast, _Fade;
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert(float4 v : POSITION) { v2f o; o.pos = UnityObjectToClipPos(v); o.dir = v.xyz; return o; }
            fixed4 frag(v2f i) : SV_Target {
                fixed3 c = texCUBE(_Cube, normalize(i.dir)).rgb;
                c = pow(saturate(c), _Contrast) * _Brightness * _Fade;   // dark space, soft stars, no glare
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
}
