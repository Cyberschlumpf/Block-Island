using UnityEngine;
public class GameGUI : GUIScreen {
 void Start(){VoxelBoxSettings.Apply();}
 void OnGUI(){VoxelBoxUI.BeginResponsive();Vector2 c=new Vector2(VoxelBoxUI.RefW*.5f,VoxelBoxUI.RefH*.5f);GUI.Label(new Rect(c.x-18,c.y-22,36,44),"+",new GUIStyle(VoxelBoxUI.Header){alignment=TextAnchor.MiddleCenter,fontSize=24});VoxelBoxUI.EndResponsive();}
 void Update(){if(GameState.IsPause)SetScreen<PauseGUI>();}
}
