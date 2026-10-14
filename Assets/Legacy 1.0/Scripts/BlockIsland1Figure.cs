using System.Collections.Generic;
using UnityEngine;

// Marks one placed copy of a Block Island 1.0 figure/object block and positions it vertically:
// automatic = lowest model point on the block floor, or a manual height (key J) between block bottom and top.
public class BlockIsland1Figure : MonoBehaviour {
    public static readonly List<BlockIsland1Figure> All = new List<BlockIsland1Figure>();

    public Block block;
    public GameObject prefab;
    public float floorY;     // world Y of the bottom of this figure's voxel

    void OnEnable() { All.Add(this); BlockIsland1HeightMenu.Ensure(); }
    void OnDisable() { All.Remove(this); }

    public void Apply() {
        float contact;
        if(!BlockIsland1Placement.TryContact(prefab, out contact)) return;   // still being measured
        float h; bool manual = BlockIsland1FigureHeights.TryGet(block, out h);
        float pivotY = float.IsNaN(contact) ? floorY + (manual ? h : .5f)        // 1.0 set point: pivot at block centre
                                            : floorY + (manual ? h : 0f) - contact;
        Vector3 p = transform.position; p.y = pivotY; transform.position = p;
    }

    public static void ApplyAll(GameObject prefab) { foreach(BlockIsland1Figure f in All.ToArray()) if(f != null && f.prefab == prefab) f.Apply(); }
    public static void ApplyAll(Block block) { foreach(BlockIsland1Figure f in All.ToArray()) if(f != null && f.block == block) f.Apply(); }
}
