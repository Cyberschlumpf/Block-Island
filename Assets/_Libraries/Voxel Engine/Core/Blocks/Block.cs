using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

public abstract class Block : ScriptableObject {
	
	public int glow;
	public Texture2D icon;

	internal int blockID;
	
	public virtual void Init(BlockSet blockSet, int blockID) {
		this.blockID = blockID;
		Face[] faces = Face.GetFaceList(this);
		
		if(blockSet != null) {
			foreach(Face face in faces) {
				face.materialID = blockSet.AddMaterial(face.material);
			}
		}
	}

    public abstract void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk);
    public abstract MeshBuilder Build();
	
	
	public virtual bool CanCreateBlock(Vector3i pos) {
		return true;
	}
	public virtual void OnBlockCreate(Vector3i pos, BlockDirection dir) {
	}
	public virtual void OnBlockDestroy(Vector3i pos) {
	}
	public virtual void OnNeighborBlockChanged(Vector3i pos, Vector3i neighborPos) {
	}
	
	public bool DrawPreview(Rect position) {
		if(icon != null) {
			GUI.DrawTexture(position, icon);
		} else {
			Face face = GetPreviewFace();
			Texture texture = face != null ? face.GetTexture() : null;
			if(texture != null) {
				GUI.DrawTextureWithTexCoords(position, texture, face.rect);
			}
		}
		return Event.current.IsMouseDown(0) && position.Contains(Event.current.mousePosition);
	}
	public virtual Face GetPreviewFace() {
		return null;
	}
	
	
	public virtual bool IsSolid() {
		return true;
	}
	
	public virtual bool IsAlpha() {
		return true;
	}
	
	public virtual int GetLightStep() {
		return 2;
	}
	
}