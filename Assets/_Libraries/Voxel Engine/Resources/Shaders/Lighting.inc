
float3 Lighting(float3 color, float4 lightData) {
	float3 glow = lightData.xxx;
    float sun = lightData.y;
    float3 ambient = UNITY_LIGHTMODEL_AMBIENT * 2 * sun;
      		
    ambient = max(ambient, 0.0666);
    ambient = max(ambient, glow);
    // Stage 6.11.10: keep legacy voxel/GI light, but stop it overpowering realtime sun shadows.
	// Direct Lambert lighting remains fully active; this is only the self-lit legacy contribution.
	return color * ambient * 0.35;
}