using UnityEngine;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;

[System.Serializable]
public class Face {
	
	public Material material;
	public int materialID;
	public Rect rect;
	
	public void Set(Face face) {
		material = face.material;
		materialID = face.materialID;
		rect = face.rect;
	}
	
	public Texture2D GetTexture() {
		if(material) return material.mainTexture as Texture2D;
		return null;
	}
	
	
	
	
	public static Face[] GetFaceList(Block block) {
		List<FieldInfo> fields = GetFields<Face>( block );
		List<Face> list = new List<Face>();
		foreach(FieldInfo f in fields) {
			list.Add( (Face) f.GetValue( block ) ); 
		}
		return list.ToArray();
	}
	
	public static string[] GetFaceNameList(Block block) {
		List<FieldInfo> fields = GetFields<Face>( block );
		List<string> list = new List<string>();
		foreach(FieldInfo f in fields) {
			list.Add( FixNameString(f.Name) );
		}
		return list.ToArray();
	}
	
	
	private static string FixNameString(string name) {
		var array = name.ToCharArray();
		array[0] = char.ToUpper( array[0] );
		return new string( array );
	}
	
	private static List<FieldInfo> GetFields<T>(object obj) {
		var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		FieldInfo[] fields = obj.GetType().GetFields( flags );
		var list = new List<FieldInfo>();
		foreach(FieldInfo field in fields) {
			if(field.FieldType == typeof(T)) list.Add(field);
		}
		return list;
	}
	
	
	
}