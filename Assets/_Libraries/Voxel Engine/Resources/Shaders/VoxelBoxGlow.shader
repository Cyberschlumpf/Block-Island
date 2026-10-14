Shader "VoxelEngine/VoxelBoxGlow" {
 Properties { _MainTex ("Base (RGB)", 2D) = "white" {} _GlowColor ("Glow", Color) = (1.0,0.68,0.22,1) _GlowStrength ("Strength", Range(0,3)) = 1.25 }
 SubShader { Tags { "RenderType"="Opaque" } LOD 200
 CGPROGRAM
 #pragma surface surf Lambert noambient addshadow fullforwardshadows
 #include "Lighting.inc"
 sampler2D _MainTex; fixed4 _GlowColor; half _GlowStrength;
 struct Input { float2 uv_MainTex; float4 color : COLOR; };
 void surf(Input IN, inout SurfaceOutput o) { fixed4 c=tex2D(_MainTex,IN.uv_MainTex); o.Albedo=c.rgb; o.Emission=c.rgb*_GlowColor.rgb*_GlowStrength + Lighting(c.rgb,IN.color); o.Alpha=1; }
 ENDCG }
 FallBack "Diffuse"
}
