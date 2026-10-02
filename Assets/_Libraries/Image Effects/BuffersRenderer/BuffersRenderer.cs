using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class BuffersRenderer : MonoBehaviour {
	
	public enum RenderType {
		DepthAndNormals_Depth,
		DepthAndNormals_Normals,
		Depth,
		Normals,
	}
	
	public RenderType type = RenderType.DepthAndNormals_Depth;
	
	private Material material;
	
	void OnEnable() {
		material = new Material( Shader.Find("Hidden/BuffersRenderer") );
	}
	
	void Update() {
		if( type == RenderType.DepthAndNormals_Depth ) {
			GetComponent<Camera>().depthTextureMode = DepthTextureMode.DepthNormals;
		}
		if( type == RenderType.DepthAndNormals_Normals ) {
			GetComponent<Camera>().depthTextureMode = DepthTextureMode.DepthNormals;
		}
		if( type == RenderType.Depth ) {
			GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
		}
	}
	
	[ImageEffectOpaque] // вызывает постобработку перед overlay очередью
	void OnRenderImage(RenderTexture src, RenderTexture dest) {
		if( type == RenderType.DepthAndNormals_Depth ) {
			Graphics.Blit( src, dest, material, 0 );
		}
		if( type == RenderType.DepthAndNormals_Normals ) {
			Graphics.Blit( src, dest, material, 1 );
		}
		if( type == RenderType.Depth ) {
			Graphics.Blit( src, dest, material, 2 );
		}
		if( type == RenderType.Normals ) {
			Graphics.Blit( src, dest, material, 3 );
		}
	}
	
	
}
