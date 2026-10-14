using UnityEngine;

public class GlassBlock1 : Block
{
	public static readonly Box box = Box.CenterSize(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1f, 0.2f, 0.2f));

	public static readonly Box yBox = Box.CenterSize(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1f, 0.2f, 1f));

	public static readonly Box zBox = Box.CenterSize(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.2f, 1f, 1f));

	public Face face;

	public override void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk)
	{
		int materialID = face.materialID;
		Box box = GlassBlock1.box;
		Box box2 = yBox;
		Box box3 = zBox;
		bool flag = IsBackSolid(index, chunk);
		bool flag2 = IsFrontSolid(index, chunk);
		bool flag3 = IsLeftSolid(index, chunk);
		bool flag4 = IsRightSolid(index, chunk);
		if (!flag3)
		{
			box2.min.y = 0.5f;
		}
		if (!flag4)
		{
			box2.max.y = 0.5f;
		}
		if (!flag)
		{
			box3.min.z = 0.5f;
		}
		if (!flag2)
		{
			box3.max.z = 0.5f;
		}
		if (!flag3 && !flag4 && !flag && !flag2)
		{
			builder.AddBox(box, face.rect, pos, materialID);
		}
		if (flag3 || flag4)
		{
			builder.AddBox(box2, face.rect, pos, materialID);
		}
		if (flag || flag2)
		{
			builder.AddBox(box3, face.rect, pos, materialID);
		}
	}

	private static void BuildFace(MeshBuilder builder, int index, Face face, BlockDirection direction, LocalPosition pos)
	{
	}

	public static bool IsFrontSolid(int index, Chunk chunk)
	{
		Block block = chunk.GetFrontBlock(index).block;
		return block is GlassBlock1 || block is GroundBlock || block is CubeBlock;
	}

	public static bool IsBackSolid(int index, Chunk chunk)
	{
		Block block = chunk.GetBackBlock(index).block;
		return block is GlassBlock1 || block is GroundBlock || block is CubeBlock;
	}

	public static bool IsRightSolid(int index, Chunk chunk)
	{
		Block block = chunk.GetRightBlock(index).block;
		return block is GlassBlock1 || block is GroundBlock || block is CubeBlock;
	}

	public static bool IsLeftSolid(int index, Chunk chunk)
	{
		Block block = chunk.GetLeftBlock(index).block;
		return block is GlassBlock1 || block is GroundBlock || block is CubeBlock;
	}

	public override MeshBuilder Build()
	{
		MeshBuilder meshBuilder = new MeshBuilder();
		meshBuilder.AddBox(box, face.rect, LocalPosition.zero, face.materialID);
		meshBuilder.AddBox(yBox, face.rect, LocalPosition.zero, face.materialID);
		meshBuilder.AddBox(zBox, face.rect, LocalPosition.zero, face.materialID);
		return meshBuilder;
	}

	public override Face GetPreviewFace()
	{
		return face;
	}
}
