using UnityEngine;

public class LadderBlock1 : Block
{
	public static readonly Box box = Box.CenterSize(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.4f, 1f, 0.4f));

	public static readonly Box xBox1 = Box.CenterSize(new Vector3(0.5f, 0.76f, 0.5f), new Vector3(0.6f, 0.1f, 0.1f));

	public static readonly Box xBox2 = Box.CenterSize(new Vector3(0.5f, 0.24f, 0.5f), new Vector3(0.6f, 0.1f, 0.1f));

	public static readonly Box zBox1 = Box.CenterSize(new Vector3(0.5f, 0.76f, 0.5f), new Vector3(0.1f, 0.1f, 0.6f));

	public static readonly Box zBox2 = Box.CenterSize(new Vector3(0.5f, 0.24f, 0.5f), new Vector3(0.1f, 0.1f, 0.6f));

	public Face tycka;

	public Face priecka;

	public Face waterTop;

	public Face waterBottom;

	public override void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk)
	{
		Box box = LadderBlock1.box;
		Box box2 = xBox1;
		Box box3 = xBox2;
		Box box4 = zBox1;
		Box box5 = LadderBlock.zBox2;
		builder.AddBox(box, tycka.rect, pos, tycka.materialID);
	}

	private static void BuildFace(MeshBuilder builder, int index, Face face, BlockDirection direction, LocalPosition pos)
	{
	}

	public override MeshBuilder Build()
	{
		MeshBuilder meshBuilder = new MeshBuilder();
		meshBuilder.AddBox(box, tycka.rect, LocalPosition.zero, tycka.materialID);
		meshBuilder.AddBox(xBox1, priecka.rect, LocalPosition.zero, priecka.materialID);
		meshBuilder.AddBox(xBox2, priecka.rect, LocalPosition.zero, priecka.materialID);
		meshBuilder.AddBox(zBox1, priecka.rect, LocalPosition.zero, priecka.materialID);
		meshBuilder.AddBox(zBox2, priecka.rect, LocalPosition.zero, priecka.materialID);
		return meshBuilder;
	}

	public override Face GetPreviewFace()
	{
		return tycka;
	}
}
