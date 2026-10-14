using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

[CustomEditor( typeof( Atlas ) )]
public class AtlasEditor : Editor {

    private int tileIndex;

    [MenuItem( "Assets/Create/Atlas" )]
    public static void CreateAtlas() {
        BlockEditor.CreateAsset<Atlas>();
    }


    public override void OnInspectorGUI() {
        Atlas atlas = (Atlas) target;

        atlas.width = EditorGUILayout.IntField( "Width", atlas.width );
        atlas.height = EditorGUILayout.IntField( "Height", atlas.height );

        atlas.width = Mathf.Max( atlas.width, 16 );
        atlas.height = Mathf.Max( atlas.height, 16 );

        atlas.width = Mathf.ClosestPowerOfTwo( atlas.width );
        atlas.height = Mathf.ClosestPowerOfTwo( atlas.height );

        EditorGUILayout.Separator();
        DrawTileList( ref tileIndex, atlas );
        EditorGUILayout.Separator();

        DrawAtlas( ref tileIndex, atlas );

        if( GUILayout.Button("Pack") ) {
            Texture2D texture = new Texture2D( atlas.width, atlas.height, TextureFormat.RGBA32, true );
            ApplyAtlas( texture, atlas );

            string path = AssetDatabase.GetAssetPath( atlas );
            path = Path.GetDirectoryName( path ) + '/' + Path.GetFileNameWithoutExtension( path ) + ".png";
            string fullPath = Path.Combine( Directory.GetCurrentDirectory(), path );

            File.WriteAllBytes( fullPath, texture.EncodeToPNG() );
            AssetDatabase.ImportAsset(path);
        }

        if(GUI.changed) {
            EditorUtility.SetDirty( atlas );
        }
    }

    private static void DrawTileList(ref int index, Atlas atlas) {
        List<Atlas.Tile> tiles = atlas.tiles;

        GUILayout.BeginVertical(GUI.skin.box);

        if(tiles.Count == 0) {
            GUILayout.Label( "Texture list is empty" );
        } else {
            index = EditorGUIUtils.DrawList( index, tiles );
            index = Mathf.Clamp( index, 0, tiles.Count - 1 );
            EditorGUILayout.Separator();

            using( new VerticalLayout(GUI.skin.box) ) {
                Atlas.Tile tile = tiles[index];
                using(new HorizontalLayout()) {
                    tile.texture = EditorGUIUtils.AssetField<Texture2D>( tile.texture );
                    if(GUILayout.Button( "Remove" )) {
                        tiles.RemoveAt( index );
                    }
                }
                tile.x = EditorGUILayout.IntField( "X", tile.x );
                tile.y = EditorGUILayout.IntField( "Y", tile.y );
            }
        }

        EditorGUILayout.Separator();

        using(new HorizontalLayout()) {
            GUILayout.Label( "Add New Texture" );
            Texture2D newTexture = (Texture2D) EditorGUILayout.ObjectField( null, typeof( Texture2D ), false );
            if(newTexture != null && !atlas.Contains(newTexture)) {
                tiles.Add( new Atlas.Tile( newTexture ) );
                index = tiles.Count - 1;
            }
        }

        GUILayout.EndVertical();
    }


    private static void DrawAtlas(ref int tileIndex, Atlas atlas) {
        GUILayout.BeginVertical( GUI.skin.box );
        Rect rect = GUILayoutUtility.GetAspectRect( (float) atlas.width / atlas.height, GUILayout.MaxWidth( atlas.width ), GUILayout.MaxHeight( atlas.height ) );
        GUILayout.EndVertical();

        Vector2 scale = new Vector2( rect.width / atlas.width, rect.height / atlas.height );

        GUIUtils.FillRect( rect, Color.red );
        using( new Group(rect) ) {
            var tiles = atlas.tiles;

            for(int i = 0; i < tiles.Count; i++) {
                var tile = tiles[i];
                if(tile.texture) {
                    Rect pos = new Rect( tile.x, tile.y, tile.texture.width, tile.texture.height );
                    pos = RectUtils.Scale( pos, scale );
                    pos.y = (rect.height - pos.y) - pos.height;

                    if(Event.current.IsMouseDown( 0 ) && pos.Contains( Event.current.mousePosition )) {
                        tileIndex = atlas.tiles.IndexOf( tile );
                        Event.current.Use();
                    }
                }
            }

            for(int i=0; i < tiles.Count; i++) {
                var tile = tiles[i];
                if(tile.texture) {
                    Rect pos = new Rect( tile.x, tile.y, tile.texture.width, tile.texture.height );
                    pos = RectUtils.Scale( pos, scale );
                    pos.y = (rect.height - pos.y) - pos.height;
                    
                    if(Event.current.IsMouseDrag( 0 ) && pos.Contains( Event.current.mousePosition ) && tileIndex == i) {
                        tile.x += (int) Event.current.delta.x;
                        tile.y -= (int) Event.current.delta.y;
                        GUI.changed = true;
                        Event.current.Use();
                    }
                    if(Event.current.IsMouseUp( 0 ) && pos.Contains( Event.current.mousePosition ) && tileIndex == i) {
                        tile.x = Mathf.RoundToInt( tile.x / 16f ) * 16;
                        tile.y = Mathf.RoundToInt( tile.y / 16f ) * 16;
                        GUI.changed = true;
                        Event.current.Use();
                    }

                    GUI.DrawTexture( pos, tile.texture );
                }
            }
        }
    }


    private static void ApplyAtlas(Texture2D texture, Atlas atlas) {
        for(int x = 0; x < texture.width; x++) {
            for(int y = 0; y < texture.height; y++) {
                texture.SetPixel(x, y, new Color());
            }
        }
        foreach(var tile in atlas.tiles) {
            Texture2D tex = tile.texture;
            if(tex != null) {
                Color[] pixels = tex.GetPixels(0, 0, tex.width, tex.height);
                texture.SetPixels(tile.x, tile.y, tex.width, tex.height, pixels);
            }
        }
        texture.Apply();
    }

	
}
