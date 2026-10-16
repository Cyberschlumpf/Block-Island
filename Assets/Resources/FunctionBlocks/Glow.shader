// Glowing parts of function blocks (lamp glass, teleporter ring): unlit colour times brightness, with fog.
Shader "Block Island/Glow" {
    Properties { _Color ("Color", Color) = (1,1,1,1) _Intensity ("Intensity", Range(0, 4)) = 1 }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            fixed4 _Color; float _Intensity;
            struct v2f { float4 pos : SV_POSITION; UNITY_FOG_COORDS(0) };
            v2f vert(float4 v : POSITION) { v2f o; o.pos = UnityObjectToClipPos(v); UNITY_TRANSFER_FOG(o, o.pos); return o; }
            fixed4 frag(v2f i) : SV_Target { fixed4 c = fixed4(_Color.rgb * _Intensity, 1); UNITY_APPLY_FOG(i.fogCoord, c); return c; }
            ENDCG
        }
    }
}
