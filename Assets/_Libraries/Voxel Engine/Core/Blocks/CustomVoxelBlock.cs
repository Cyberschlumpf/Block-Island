using UnityEngine;
using System;
using System.IO;

// The Voxel Box 0.9.9.5 - 8x8x8 player-authored voxel sculpture + FortressCraft-inspired visual motion.
public class CustomVoxelBlock : Block {
    public const int GRID=8, COUNT=GRID*GRID*GRID;
    public enum Motion { None, RotateCW, RotateCCW, Twizzle, Wind, Dangle, Jiggle, Dervish, Piston, Bounce, Squish, GrowShrink, Orbit, Planetary, RandomiseFacing, Big, Small, Hover, SpinX, SpinZ, Figure8, Rock, Pulse }
    public enum ParticleFx { None, Fire, Smoke, GreenSmoke, BlueSmoke, PurpleSmoke, Fountain, Sparkles, Magic, Steam, GlowingDust, Embers, FireSmall, FireLarge, MagicSwirl, MagicBurst, BlueMagic, PurpleMagic, GoldenSparkles, SnowDust, Ash, Leaves, Bubbles, WaterMist, DrippingWater, ElectricSparks, EnergyPulse }
    public int slot;
    public Motion animation=Motion.None;
    public float animationSpeed=1f;
    public ParticleFx particleFx=ParticleFx.None;
    public float particleStrength=1f;
    [NonSerialized] public DataBlock[] design=new DataBlock[COUNT];

    [Serializable] class DesignFile { public int version=2; public int slot; public int[] blockID=new int[COUNT]; public int[] direction=new int[COUNT]; public int animation; public float animationSpeed=1f; public int particleFx; public float particleStrength=1f; }
    public override void Init(BlockSet set,int id) { base.Init(set,id); LoadDesign(); }
    public bool HasDesign { get { for(int i=0;i<design.Length;i++) if(!design[i].IsEmpty()) return true; return false; } }
    public void SetDesign(DataBlock[] src) { design=new DataBlock[COUNT]; Array.Copy(src,design,Mathf.Min(COUNT,src.Length)); SaveDesign(); }
    public void CycleAnimation(int delta){int n=Enum.GetValues(typeof(Motion)).Length; animation=(Motion)(((int)animation+delta+n)%n);SaveDesign();}
    public void CycleParticle(int delta){int n=Enum.GetValues(typeof(ParticleFx)).Length; particleFx=(ParticleFx)(((int)particleFx+delta+n)%n);SaveDesign();}
    public void SaveSettings(){SaveDesign();}

    static string Folder { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"The Voxel Box","CustomBlocks"); } }
    string FilePath { get { return Path.Combine(Folder,"CustomVoxel_"+(slot+1).ToString("00")+".json"); } }
    void SaveDesign(){ try { Directory.CreateDirectory(Folder); DesignFile f=new DesignFile(); f.slot=slot;f.animation=(int)animation;f.animationSpeed=animationSpeed; f.particleFx=(int)particleFx; f.particleStrength=particleStrength; for(int i=0;i<COUNT;i++){f.blockID[i]=design[i].blockID;f.direction[i]=(int)design[i].direction;} File.WriteAllText(FilePath,JsonUtility.ToJson(f,true)); } catch(Exception e){Debug.LogWarning("Custom block save failed: "+e.Message);} }
    void LoadDesign(){ try { if(!File.Exists(FilePath)) return; DesignFile f=JsonUtility.FromJson<DesignFile>(File.ReadAllText(FilePath)); if(f==null||f.blockID==null) return; animation=(Motion)Mathf.Clamp(f.animation,0,Enum.GetValues(typeof(Motion)).Length-1);animationSpeed=f.animationSpeed<=0?1f:f.animationSpeed; particleFx=(ParticleFx)Mathf.Clamp(f.particleFx,0,Enum.GetValues(typeof(ParticleFx)).Length-1); particleStrength=f.particleStrength<=0?1f:f.particleStrength; for(int i=0;i<COUNT&&i<f.blockID.Length;i++){design[i].blockID=f.blockID[i]; if(f.direction!=null&&i<f.direction.Length) design[i].direction=(BlockDirection)f.direction[i];} } catch(Exception e){Debug.LogWarning("Custom block load failed: "+e.Message);} }

    int I(int x,int y,int z){return x+y*GRID+z*GRID*GRID;} bool Filled(int x,int y,int z){return x>=0&&y>=0&&z>=0&&x<GRID&&y<GRID&&z<GRID&&!design[I(x,y,z)].IsEmpty();}
    Face FaceFor(DataBlock d,CubeSide side){ Face atlasOverride=BlockTextureDesigner.CustomVoxelAtlasFace(this);if(atlasOverride!=null)return atlasOverride; CubeBlock c=d.block as CubeBlock; if(c!=null){ CubeSide s=CubeBlock.TransformSide(side,d.direction); if(s==CubeSide.Front)return c.front;if(s==CubeSide.Back)return c.back;if(s==CubeSide.Right)return c.right;if(s==CubeSide.Left)return c.left;if(s==CubeSide.Top)return c.top;return c.bottom;} return d.block!=null?d.block.GetPreviewFace():null; }
    void Quad(MeshBuilder b,Vector3 a,Vector3 bb,Vector3 c,Vector3 d,Vector3 n,Face f,LocalPosition pos,BlockDirection dir){ if(f==null||f.materialID<0)return; b.AddFaceIndices(f.materialID); Vector3 p=(Vector3)pos; Vector3[] q={a,bb,c,d}; for(int i=0;i<4;i++) b.AddVertex(BlockDirectionUtils.TransformBlockVertex(q[i],dir)+p); b.AddFaceNormal(n,dir); b.AddTexCoords(f.rect); }
    public override void Build(MeshBuilder b,DataBlock block,LocalPosition pos,int index,Chunk chunk){ const float s=1f/GRID; for(int z=0;z<GRID;z++)for(int y=0;y<GRID;y++)for(int x=0;x<GRID;x++){DataBlock d=design[I(x,y,z)];if(d.IsEmpty())continue;float x0=x*s,x1=(x+1)*s,y0=y*s,y1=(y+1)*s,z0=z*s,z1=(z+1)*s;if(!Filled(x,y,z-1))Quad(b,new Vector3(x0,y0,z0),new Vector3(x0,y1,z0),new Vector3(x1,y1,z0),new Vector3(x1,y0,z0),Vector3.back,FaceFor(d,CubeSide.Back),pos,block.direction);if(!Filled(x,y,z+1))Quad(b,new Vector3(x1,y0,z1),new Vector3(x1,y1,z1),new Vector3(x0,y1,z1),new Vector3(x0,y0,z1),Vector3.forward,FaceFor(d,CubeSide.Front),pos,block.direction);if(!Filled(x-1,y,z))Quad(b,new Vector3(x0,y0,z1),new Vector3(x0,y1,z1),new Vector3(x0,y1,z0),new Vector3(x0,y0,z0),Vector3.left,FaceFor(d,CubeSide.Left),pos,block.direction);if(!Filled(x+1,y,z))Quad(b,new Vector3(x1,y0,z0),new Vector3(x1,y1,z0),new Vector3(x1,y1,z1),new Vector3(x1,y0,z1),Vector3.right,FaceFor(d,CubeSide.Right),pos,block.direction);if(!Filled(x,y+1,z))Quad(b,new Vector3(x0,y1,z0),new Vector3(x0,y1,z1),new Vector3(x1,y1,z1),new Vector3(x1,y1,z0),Vector3.up,FaceFor(d,CubeSide.Top),pos,block.direction);if(!Filled(x,y-1,z))Quad(b,new Vector3(x0,y0,z1),new Vector3(x0,y0,z0),new Vector3(x1,y0,z0),new Vector3(x1,y0,z1),Vector3.down,FaceFor(d,CubeSide.Bottom),pos,block.direction);}}
    public override MeshBuilder Build(){ if(!HasDesign)return null; MeshBuilder b=new MeshBuilder(); Build(b,new DataBlock(this,BlockDirection.FORWARD),LocalPosition.zero,0,null); return b;} public override Face GetPreviewFace(){Face atlasOverride=BlockTextureDesigner.CustomVoxelAtlasFace(this);if(atlasOverride!=null)return atlasOverride;for(int i=0;i<COUNT;i++)if(!design[i].IsEmpty())return FaceFor(design[i],CubeSide.Front);return null;} // 0.9.9.5e: A custom sculpture is not guaranteed to fill its 1x1x1 voxel cell.
    // Treat it as alpha/non-occluding for neighbour face decisions so a normal block below/
    // beside an open custom model keeps its texture instead of losing that face.
    public override bool IsAlpha(){return true;}
}
