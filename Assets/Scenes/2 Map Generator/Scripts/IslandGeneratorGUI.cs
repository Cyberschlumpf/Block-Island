using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class IslandGeneratorGUI : MonoBehaviour {

	public const int PREVIEW_SIZE = 256;

	private IslandGenerator island;
	
	private Gradient gradient;
	private Color[] pixels;
	private Texture2D texture;
	

	void Start() {
		island = GetComponent<IslandGenerator>();

		gradient = new Gradient();
		gradient.colorKeys = new GradientColorKey[] {
			new GradientColorKey(Color.blue/2f,                 0),
			new GradientColorKey(Color.blue,                    IslandGenerator.WATER_LEVEL-0.04f),
			new GradientColorKey(new Color(0, 0.5f, 1f),        IslandGenerator.WATER_LEVEL),

			new GradientColorKey(Color.yellow,                  IslandGenerator.WATER_LEVEL),
			new GradientColorKey(Color.yellow,                  IslandGenerator.SAND_LEVEL),
			new GradientColorKey(Color.green,                   IslandGenerator.DIRT_LEVEL),
			new GradientColorKey(new Color32(139, 90, 43, 255), IslandGenerator.ROCK_LEVEL),
			new GradientColorKey(Color.black,                   1)
		};

		pixels = new Color[PREVIEW_SIZE * PREVIEW_SIZE];
		texture = new Texture2D( PREVIEW_SIZE, PREVIEW_SIZE, TextureFormat.RGB24, false );
		texture.filterMode = FilterMode.Trilinear;
		UpdatePreview();
	}

	void OnDestroy() {
		Destroy( texture );
	}

	void OnGUI() {
        VoxelBoxUI.BeginResponsive(); VoxelBoxUI.MenuBackground(); VoxelBoxUI.Backdrop(.20f);
        Rect outer=VoxelBoxUI.Safe(1120,820,35);
        GUILayout.BeginArea(outer,VoxelBoxUI.Panel);
        GUILayout.Label("BLOCK ISLAND",VoxelBoxUI.Header);
        GUILayout.Label("INSELGENERATOR  •  NEUE CREATIVE WORLD",VoxelBoxUI.Small);
        GUILayout.Space(12);
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(650));
        GUILayout.Label("WELTVORSCHAU",VoxelBoxUI.Header);
        Rect preview=GUILayoutUtility.GetRect(610,610,GUILayout.ExpandWidth(false)); DrawNoise(preview,texture,island);
        GUILayout.EndVertical(); GUILayout.Space(24);
        GUILayout.BeginVertical();
        GUILayout.Label("WELT",VoxelBoxUI.Header); GUILayout.Label("Ziehe in der Vorschau zum Verschieben. Mausrad zoomt die Inselstruktur.",VoxelBoxUI.Label);
        GUILayout.Space(16); GUILayout.Label("WELTTYP",VoxelBoxUI.Header);
        bool sky=GUILayout.Toggle(island.skyIslands,"  SKY-INSELN  •  schwebende, rundum bebaubare Inseln",VoxelBoxUI.Label);
        if(sky!=island.skyIslands){ island.skyIslands=sky; }
        GUILayout.Label(island.skyIslands ? "3D-Voxelkörper mit Felsunterseite. Oben, unten und seitlich bebaubar." : "Klassische Inselwelt mit Meer.",VoxelBoxUI.Small);
        GUILayout.Space(16); GUILayout.Label("GRAFIK",VoxelBoxUI.Header); GUILayout.Label("Sichtweite: "+VoxelBoxSettings.ViewDistance+" Chunks",VoxelBoxUI.Label); VoxelBoxSettings.ViewDistance=Mathf.RoundToInt(GUILayout.HorizontalSlider(VoxelBoxSettings.ViewDistance,4,32));
        bool sh=GUILayout.Toggle(VoxelBoxSettings.Shadows,"  Schatten",VoxelBoxUI.Label); if(sh!=VoxelBoxSettings.Shadows)VoxelBoxSettings.Shadows=sh;
        bool we=GUILayout.Toggle(VoxelBoxSettings.WaterEffects,"  Wassereffekte / Wellen",VoxelBoxUI.Label); if(we!=VoxelBoxSettings.WaterEffects)VoxelBoxSettings.WaterEffects=we;
        GUILayout.FlexibleSpace(); if(GUILayout.Button("▶  WELT ERSTELLEN",VoxelBoxUI.Button,GUILayout.Height(52))){InfiniteWorldLaunchConfig.Capture(island);if(BlockSet.instance!=null){Transform bs=BlockSet.instance.transform;if(bs.parent!=null)bs.SetParent(null,true);DontDestroyOnLoad(bs.gameObject);}SceneManager.LoadScene("Game");}
        GUILayout.EndVertical(); GUILayout.EndHorizontal(); GUILayout.EndArea(); VoxelBoxUI.EndResponsive();
	}

	private void UpdatePreview() {
		//float t1 = Time.realtimeSinceStartup;

		Tasks.ParallelFor (UpdatePreview, PREVIEW_SIZE);
		texture.SetPixels (pixels);
		texture.Apply ();

		//Debug.Log ( Time.realtimeSinceStartup - t1 );
	}

	private void UpdatePreview(int fromY, int toY) {
		int scaleX = island.sizeX / PREVIEW_SIZE;
		int scaleY = island.sizeZ / PREVIEW_SIZE;

		for(int y=fromY; y<toY; y++) {
			for(int x=0; x<PREVIEW_SIZE; x++) {
				float h = island.GetNoise(x*scaleX, y*scaleY);
				pixels[y*PREVIEW_SIZE+x] = gradient.Evaluate(h);
			}
		}
	}

	private Vector2 DrawNoise(Rect rect, Texture2D texture, IslandGenerator island) {
		GUI.DrawTexture(rect, texture);

		FastPerlinNoise2D noise = island.noise;
		int id = GUIUtility.GetControlID(FocusType.Passive, rect);
		Matrix4x4 invMatrix = MatrixUtils.Create (rect.x, rect.yMax, rect.width/island.sizeX, -rect.height/island.sizeZ).inverse;

		if( Event.current.IsMouseDown() && rect.Contains(Event.current.mousePosition) ) {
			GUIUtility.hotControl = id;
			Event.current.Use();
		}

		if( Event.current.IsMouseDrag() && GUIUtility.hotControl == id ) {
			Vector2 delta = invMatrix.MultiplyVector( Event.current.delta );
			noise.offset -= delta * noise.scale;

			UpdatePreview();
			Event.current.Use();
		}

		if( Event.current.IsMouseUp() && GUIUtility.hotControl == id ) {
			GUIUtility.hotControl = 0;
			Event.current.Use();
		}
		
		if( Event.current.IsScrollWheel() && rect.Contains(Event.current.mousePosition) ) {
			Vector2 mouse = invMatrix.MultiplyPoint( Event.current.mousePosition );
			
			noise.offset += mouse * noise.scale;
			if(Event.current.delta.y > 0) noise.scale *= 1.1f;
			if(Event.current.delta.y < 0) noise.scale /= 1.1f;
			noise.offset -= mouse * noise.scale;
			
			UpdatePreview();
			Event.current.Use();
		}

		return Vector2.zero;
	}

}
