// Built-in render pipeline stand-in for Hovl Studio's Shader Graph particle shaders (HS_Blend_CG, HS_FlareFX,
// HS_LitFresnel ...), which only exist for URP/HDRP. Uses the same property names, so the converted materials keep
// their textures, colours and scroll speeds. Additive, unlit, works in Gamma colour space.
Shader "Block Island/Hovl Particles" {
    Properties {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Noise ("Noise", 2D) = "white" {}
        _Flow ("Flow", 2D) = "gray" {}
        _Mask ("Mask", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _Emission ("Emission", Float) = 2
        _Opacity ("Opacity", Float) = 1
        _SpeedMainTexUVNoiseZW ("Speed MainTex XY / Noise ZW", Vector) = (0,0,0,0)
        _DistortionSpeedXYPowerZ ("Distortion Speed XY / Power Z", Vector) = (0,0,0,0)
        _Noise_speed_XY ("Noise Speed XY", Vector) = (0,0,0,0)
        _GammaScale ("Brightness in Gamma", Float) = 0.35
    }
    SubShader {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Blend SrcAlpha One
        Cull Off Lighting Off ZWrite Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            sampler2D _MainTex, _Noise, _Flow, _Mask;
            float4 _MainTex_ST, _Noise_ST, _Flow_ST, _Mask_ST;
            fixed4 _Color, _EmissionColor;
            float _Emission, _Opacity, _GammaScale;
            float4 _SpeedMainTexUVNoiseZW, _DistortionSpeedXYPowerZ, _Noise_speed_XY;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; UNITY_FOG_COORDS(1) };
            v2f vert(appdata v) {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color; o.uv = v.uv;
                UNITY_TRANSFER_FOG(o, o.pos); return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                float t = _Time.y;
                float2 flow = tex2D(_Flow, TRANSFORM_TEX(i.uv, _Flow) + _DistortionSpeedXYPowerZ.xy * t).rg * 2 - 1;
                float2 uv = i.uv + flow * _DistortionSpeedXYPowerZ.z;
                fixed4 main = tex2D(_MainTex, TRANSFORM_TEX(uv, _MainTex) + _SpeedMainTexUVNoiseZW.xy * t);
                fixed4 noise = tex2D(_Noise, TRANSFORM_TEX(uv, _Noise) + _SpeedMainTexUVNoiseZW.zw * t + _Noise_speed_XY.xy * t);
                fixed mask = tex2D(_Mask, TRANSFORM_TEX(i.uv, _Mask)).r;
                fixed4 c = main * noise * _Color * _EmissionColor * i.color;
                c.rgb *= _Emission * _GammaScale;
                c.a = saturate(c.a * mask * _Opacity);
                UNITY_APPLY_FOG_COLOR(i.fogCoord, c, fixed4(0,0,0,0));
                return c;
            }
            ENDCG
        }
    }
}
