using UnityEngine;
using System.Collections;
using System.IO;

public static class IOUtils {

	public static readonly string savesPath = Application.dataPath + "/Saves/";

    public static string[] GetSaves() {
        return Directory.GetFiles( savesPath, "*.map" );
    }

    public static string GetNewName() {
        int num = GetSaves().Length + 1;
        return num + ".map";
    }

    public static void Save(string file, byte[] bytes) {
        Directory.CreateDirectory( savesPath );
        File.WriteAllBytes( savesPath + file, bytes );
    }

}
