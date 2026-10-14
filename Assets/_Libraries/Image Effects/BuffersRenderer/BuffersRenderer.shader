Shader "Hidden/BuffersRenderer" {
    SubShader {
        ZTest Always Cull Off ZWrite Off Fog { Mode Off }
        
        CGINCLUDE
        #include "UnityCG.cginc"
        
        sampler2D _CameraDepthNormalsTexture;
        sampler2D _CameraDepthTexture;
        sampler2D _CameraNormalsTexture;
        
        ENDCG
        
        Pass {
        CGPROGRAM
        #pragma vertex vert_img
        #pragma fragment frag
 
        float4 frag(v2f_img i) : COLOR {
            float4 data = tex2D (_CameraDepthNormalsTexture, i.uv);
            //float3 normalVS;
      		float depth;
      		//DecodeDepthNormal(data, depth, normalVS);
      		depth = DecodeFloatRG(data.zw);
      		
            return float4(depth.xxx, 1);
        }

        ENDCG
        }
        
        Pass {
        CGPROGRAM
		#pragma vertex vert_img
		#pragma fragment frag
 
        float4 frag(v2f_img i) : COLOR {
            float4 data = tex2D (_CameraDepthNormalsTexture, i.uv);
            float3 normalVS;
      		//float depth;
      		//DecodeDepthNormal(data, depth, normalVS);
      		normalVS = DecodeViewNormalStereo(data);
      		
            return float4(normalVS, 1);
        }
		
		ENDCG
        }
        
        Pass {
        CGPROGRAM
        #pragma vertex vert_img
        #pragma fragment frag
 
        float4 frag(v2f_img i) : COLOR {
            float4 data = tex2D (_CameraDepthTexture, i.uv);
      		float depth = UNITY_SAMPLE_DEPTH(data);
      		depth = Linear01Depth(depth);
      		
            return float4(depth.xxx, 1);
        }

        ENDCG
        }
        
        Pass {
        CGPROGRAM
        #pragma vertex vert_img
        #pragma fragment frag
 
        float4 frag(v2f_img i) : COLOR {
        	float4 data = tex2D(_CameraNormalsTexture, i.uv);
        	float3 normal = data.xyz * 2.0 - 1.0;
            return float4(normal, 1);
        }

        ENDCG
        }
        
    }
    FallBack Off
}