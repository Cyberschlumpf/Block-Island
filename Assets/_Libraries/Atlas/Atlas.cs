using UnityEngine;
using System.Collections.Generic;

public class Atlas : ScriptableObject {

    [System.Serializable]
    public class Tile {
        public Texture2D texture;
        public int x, y;

        public Tile(Texture2D texture) {
            this.texture = texture;
        }

        public override string ToString() {
            if(texture != null) return texture.name;
            return "null";
        }

    }

    public int width = 1024;
    public int height = 1024;
    public List<Tile> tiles = new List<Tile>();

    public bool Contains(Texture2D texture) {
        foreach(var tile in tiles) {
            if(tile.texture == texture) return true;
        }
        return false;
    }

}
