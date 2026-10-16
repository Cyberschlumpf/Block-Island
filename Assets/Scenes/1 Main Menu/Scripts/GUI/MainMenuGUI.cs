using UnityEngine; using UnityEngine.SceneManagement;
public class MainMenuGUI:GUIScreen{
 enum SidePanel{None,Settings,Controls,Guides,BlockDesigner} SidePanel side=SidePanel.None; int tab=0; Vector2 mainScroll=Vector2.zero;
 void Awake(){RestoreMenuCursor();VoxelBoxSettings.Apply();} void OnEnable(){RestoreMenuCursor();side=SidePanel.None;} void RestoreMenuCursor(){Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
 // Tanviir is a secret city: no menu button. Typing "lemuria" anywhere in the main menu (outside a text field)
 // opens it; otherwise it is reached through the portals.
 string typed="";
 void SecretCommand(){Event e=Event.current;if(e==null||e.type!=EventType.KeyDown||e.character=='\0'||GUIUtility.keyboardControl!=0)return;typed+=char.ToLowerInvariant(e.character);if(typed.Length>16)typed=typed.Substring(typed.Length-16);if(typed.EndsWith("lemuria")){typed="";InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.Tanviir);SceneManager.LoadScene("Game");}}
 void OnGUI(){SecretCommand();VoxelBoxUI.BeginResponsive();VoxelBoxUI.MenuBackground();DrawMain();if(side==SidePanel.Settings)DrawSettings();else if(side==SidePanel.Controls)DrawControlsPanel();else if(side==SidePanel.Guides)DrawGuidesPanel();else if(side==SidePanel.BlockDesigner)DrawBlockDesignerPanel();VoxelBoxUI.EndResponsive();}
 void DrawMain(){
  // 0.9.6: real visible controls over the artwork. No invisible hit targets.
  // Leicht vergroessertes Hauptmenue links unterhalb des Logos; Beenden bleibt fest sichtbar.
  // Breite +25, Hoehe +10, Position -10 gegenueber Version 2.1.16.
  Rect r=new Rect(24,265,365,630);
  GUILayout.BeginArea(r);
  GUILayout.BeginVertical(VoxelBoxUI.Panel);
  // Alle Menuepunkte ohne Scrollbereich direkt im Holzrahmen anzeigen.
  if(CompactMenuButton("◆","NEUE WELT","")){InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.Islands);SceneManager.LoadScene("Map Generator");}
  if(CompactMenuButton("▶","WELT LADEN",""))SetScreen<LoadMapGUI>();
  if(CompactMenuButton("▣","BLOCK ISLAND","")){InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.BlockIsland);SceneManager.LoadScene("Game");}
  if(CompactMenuButton("☁","SKY ISLANDS","")){InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.Islands);InfiniteWorldLaunchConfig.openSkyGenerator=true;SceneManager.LoadScene("Map Generator");}
  if(CompactMenuButton("⚙","EINSTELLUNGEN","")){tab=0;side=SidePanel.Settings;}
  if(CompactMenuButton("▦","BLOCK-DESIGNER","")){side=SidePanel.BlockDesigner;}
  GUILayout.Space(6);
  if(GUILayout.Button("✕  SPIEL BEENDEN",VoxelBoxUI.Danger,GUILayout.Height(38)))Application.Quit();
  GUILayout.EndVertical();GUILayout.EndArea();
 }
 // Kompakte Buttons ausschliesslich im Hauptmenue; andere Oberflaechen bleiben unveraendert.
 bool CompactMenuButton(string icon,string title,string sub){
  GUIStyle style=new GUIStyle(VoxelBoxUI.Button);
  style.fontSize=17;
  style.wordWrap=false;
  style.alignment=TextAnchor.MiddleLeft;
  style.padding=new RectOffset(16,8,3,3);
  return GUILayout.Button(icon+"   "+title,style,GUILayout.Height(34),GUILayout.ExpandWidth(true));
 }
 void DrawSettings(){VoxelBoxUI.Backdrop(.08f);Rect r=new Rect(500,34,1062,832);GUILayout.BeginArea(r,VoxelBoxUI.Panel);VoxelBoxUI.PanelTitle("⚙","EINSTELLUNGEN","BLOCK ISLAND  •  SYSTEM, SPIEL & ANLEITUNGEN");int oldTab=tab;tab=VoxelBoxSettingsGUI.DrawTabs(tab);if(tab!=oldTab&&tab==6)BlockIslandGuides.Reset();GUILayout.Space(8);if(tab==6)DrawGuidesInSettings();else{GUILayout.BeginVertical(VoxelBoxUI.Card);VoxelBoxSettingsGUI.Draw(tab,false);GUILayout.EndVertical();GUILayout.FlexibleSpace();}CloseRow();GUILayout.EndArea();}
 void DrawGuidesInSettings(){ BlockIslandGuides.Draw(36); GUILayout.Space(6); }
 void DrawControlsPanel(){
  VoxelBoxUI.Backdrop(.12f);Rect r=new Rect(520,44,1042,812);GUILayout.BeginArea(r,VoxelBoxUI.Panel);
  VoxelBoxUI.PanelTitle("⌨","STEUERUNG","TASTATUR & MAUS  •  CREATIVE MODE");GUILayout.Space(8);GUILayout.BeginHorizontal();
  GUILayout.BeginVertical(VoxelBoxUI.Card,GUILayout.Width(485));VoxelBoxUI.SectionTitle("MAUS & BEWEGUNG");
  GUILayout.Label("Linksklick     Block setzen / Werkzeug benutzen\nRechtsklick    Block entfernen\nMausrad        Blockauswahl wechseln\n\nW A S D        Bewegen\nSPACE          Aufwärts fliegen\nSHIFT          Abwärts fliegen\nF              Flugmodus ein/aus\nESC            Pause / Menü",VoxelBoxUI.Label);GUILayout.EndVertical();GUILayout.Space(12);
  GUILayout.BeginVertical(VoxelBoxUI.Card,GUILayout.Width(485));VoxelBoxUI.SectionTitle("CREATIVE TOOLS");
  GUILayout.Label("E              Block-Auswahl\nH              PNG-Screenshot\nM              Minecart ein-/aussteigen\nT              Zurück zum Startpunkt\nJ              Höhe einer Figur einstellen\nP              Prefab-Importer\nENTER          Funktionsblock benutzen\nN              Wegpunkte\n\nCOPY START     Erste Ecke markieren\nCOPY END       Zweite Ecke markieren\nPASTE          Auswahl einsetzen\nR bei PASTE    Auswahl +90° drehen (0°/90°/180°/270°)\nTNT            Creative-Sprengwerkzeug\nWORKSHOP       8×8×8 Custom Block",VoxelBoxUI.Label);GUILayout.EndVertical();GUILayout.EndHorizontal();
  GUILayout.Space(12);GUILayout.BeginVertical(VoxelBoxUI.Card);VoxelBoxUI.SectionTitle("EINSTELLUNGEN");GUILayout.Label("Maus-Empfindlichkeit und weitere Eingabeoptionen findest du unter Einstellungen → Steuerung.",VoxelBoxUI.Label);GUILayout.EndVertical();
  GUILayout.FlexibleSpace();CloseRow();GUILayout.EndArea();
 }

 void DrawGuidesPanel(){
  VoxelBoxUI.Backdrop(.12f);Rect r=new Rect(500,34,1062,832);GUILayout.BeginArea(r,VoxelBoxUI.Panel);
  VoxelBoxUI.PanelTitle("?","ANLEITUNGEN, HILFEN & TIPPS","BLOCK ISLAND  •  KURZ ERKLÄRT");
  BlockIslandGuides.Draw(38);
  GUILayout.Space(8);CloseRow();GUILayout.EndArea();
 }
 void DrawBlockDesignerPanel(){VoxelBoxUI.Backdrop(.12f);Rect r=new Rect(370,28,1192,844);GUILayout.BeginArea(r,VoxelBoxUI.Panel);VoxelBoxUI.PanelTitle("▦","BLOCK-DESIGNER","BLOCK-ID • GRUNDSTRUKTUR • GLOBALE TEXTUR");BlockDesignerGUI.Draw();GUILayout.FlexibleSpace();CloseRow();GUILayout.EndArea();}
 void CloseRow(){GUILayout.BeginHorizontal();GUILayout.FlexibleSpace();if(GUILayout.Button("SCHLIESSEN  ✕",VoxelBoxUI.Danger,GUILayout.Width(220),GUILayout.Height(48)))side=SidePanel.None;GUILayout.EndHorizontal();}
}
