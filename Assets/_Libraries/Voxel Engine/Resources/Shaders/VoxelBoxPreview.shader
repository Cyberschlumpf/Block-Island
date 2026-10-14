Shader "VoxelBox/PreviewUnlit" {
    Properties { _MainTex ("Texture", 2D) = "white" {} _AnimMode ("Animation Mode", Float) = 0 }
    SubShader {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off
        ZWrite On
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _AnimMode;
            v2f vert(appdata v) {
                v2f o;
                float4 p=v.vertex;
                if(_AnimMode > 1.5) {
                    // Plant/grass preview: gentle wind, strongest at the upper vertices.
                    float k=saturate(p.y+0.5)*0.12;
                    p.x += (sin(_Time.y*1.35+p.x*2.1+p.z*1.7)+sin(_Time.y*.78+p.z*2.8)*.45)*k;
                    p.z += sin(_Time.y*1.05+p.x*1.9)*k*.35;
                } else if(_AnimMode > .5) {
                    // Water preview: deliberately stronger than the in-world wave so motion is
                    // clearly visible even inside a 128x128 BlockSet thumbnail.
                    p.y += sin(p.x*3.2+_Time.y*2.10)*.060 + sin(p.z*2.7+_Time.y*1.55)*.045 + sin((p.x+p.z)*2.0+_Time.y*.95)*.025;
                }
                o.pos=UnityObjectToClipPos(p);
                o.uv=TRANSFORM_TEX(v.uv,_MainTex);
                if(_AnimMode > .5 && _AnimMode < 1.5) {
                    // Visible flowing surface in the thumbnail in addition to geometry waves.
                    o.uv += float2(_Time.y*.055, _Time.y*.028);
                    o.uv.x += sin((o.uv.y+_Time.y*.12)*12.0)*.012;
                }
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                fixed4 c=tex2D(_MainTex,i.uv);
                clip(c.a-0.05);
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
