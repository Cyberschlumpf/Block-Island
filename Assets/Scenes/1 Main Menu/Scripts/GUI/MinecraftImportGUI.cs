using UnityEngine; using UnityEngine.SceneManagement; using System; using System.IO; using System.Diagnostics;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class MinecraftImportGUI:GUIScreen {
 string path="",structurePath="",saveName="Minecraft Import";int radius=2;string status="";bool busy=false;bool fullWorld=true;Vector2 scroll;MinecraftWorldImporter.Progress progress;
 void OnGUI(){VoxelBoxUI.BeginResponsive();VoxelBoxUI.MenuBackground();VoxelBoxUI.Backdrop(.24f);Rect r=VoxelBoxUI.Safe(980,780,55);GUILayout.BeginArea(r,VoxelBoxUI.Panel);GUILayout.Label("▣  MINECRAFT-WELT IMPORTIEREN",VoxelBoxUI.Header);GUILayout.Label("Minecraft Java Edition • .schematic • .schem • .litematic → Block Island",VoxelBoxUI.Small);GUILayout.Space(10);scroll=GUILayout.BeginScrollView(scroll);GUILayout.BeginVertical(VoxelBoxUI.Card);
 GUILayout.Label("MINECRAFT-WELT",VoxelBoxUI.Header);GUILayout.Label("Wähle einfach den Ordner deiner Minecraft-Welt aus. Block Island prüft level.dat und region automatisch.",VoxelBoxUI.Label);GUILayout.Space(8);
 GUI.enabled=!busy;if(GUILayout.Button("📁  MINECRAFT-WELT AUSWÄHLEN …",VoxelBoxUI.Button,GUILayout.Height(48)))ChooseWorldFolder();if(GUILayout.Button("▦  SCHEMATIC / LITEMATIC AUSWÄHLEN …",VoxelBoxUI.Button,GUILayout.Height(48)))ChooseStructureFile();GUI.enabled=true;
 if(!String.IsNullOrEmpty(path)){GUILayout.Space(6);GUILayout.Label("Minecraft-Welt:  "+Path.GetFileName(path),VoxelBoxUI.Label);GUILayout.Label(path,VoxelBoxUI.Small);}if(!String.IsNullOrEmpty(structurePath)){GUILayout.Space(6);GUILayout.Label("Bauwerk:  "+Path.GetFileName(structurePath),VoxelBoxUI.Label);GUILayout.Label(structurePath,VoxelBoxUI.Small);}GUILayout.Space(12);
 GUILayout.Label("NAME DER BLOCK-ISLAND-WELT",VoxelBoxUI.Header);GUI.enabled=!busy;saveName=GUILayout.TextField(saveName,GUILayout.Height(38));GUILayout.Space(12);GUILayout.Label("IMPORTUMFANG",VoxelBoxUI.Header);fullWorld=GUILayout.Toggle(fullWorld," KOMPLETTE MINECRAFT-WELT IMPORTIEREN");if(!fullWorld){GUILayout.Label("Importbereich um den Spawn (Chunks)",VoxelBoxUI.Label);radius=Mathf.RoundToInt(GUILayout.HorizontalSlider(radius,1,32));GUILayout.Label(radius+" Chunks Radius",VoxelBoxUI.Small);}else GUILayout.Label("Alle vorhandenen .mca/.mcr-Regionen und belegten Chunks werden übernommen. Bei großen Welten kann dies länger dauern.",VoxelBoxUI.Small);GUI.enabled=true;GUILayout.Space(14);
 GUI.enabled=!busy&&(IsValidWorld(path)||IsValidStructure(structurePath));if(GUILayout.Button(busy?"IMPORT LÄUFT …":"IMPORTIEREN & IN BLOCK ISLAND ANSCHAUEN",VoxelBoxUI.Button,GUILayout.Height(52)))DoImport();GUI.enabled=true;
 if(busy&&progress!=null){GUILayout.Space(12);GUILayout.Label("IMPORT LÄUFT – BITTE WARTEN",VoxelBoxUI.Header);Rect bar=GUILayoutUtility.GetRect(10,28,GUILayout.ExpandWidth(true));GUI.Box(bar,"");Rect fill=new Rect(bar.x+2,bar.y+2,(bar.width-4)*Mathf.Clamp01(progress.percent),bar.height-4);GUI.Box(fill,"");GUI.Label(bar,Mathf.RoundToInt(progress.percent*100f)+" %",new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold});GUILayout.Label("Chunk "+progress.doneChunks+" / "+progress.totalChunks+"   •   "+progress.importedBlocks.ToString("N0")+" Blöcke übernommen",VoxelBoxUI.Label);}
 if(!String.IsNullOrEmpty(status)){GUILayout.Space(10);GUILayout.Label(status,VoxelBoxUI.Label);}GUILayout.EndVertical();GUILayout.EndScrollView();GUILayout.Space(8);GUI.enabled=!busy;if(GUILayout.Button("← ZURÜCK",VoxelBoxUI.Button,GUILayout.Height(44)))SetScreen<MainMenuGUI>();GUI.enabled=true;GUILayout.EndArea();VoxelBoxUI.EndResponsive();}
 bool IsValidWorld(string p){return !String.IsNullOrEmpty(p)&&Directory.Exists(p)&&File.Exists(Path.Combine(p,"level.dat"))&&Directory.Exists(Path.Combine(p,"region"));}
 bool IsValidStructure(string p){if(String.IsNullOrEmpty(p)||!File.Exists(p))return false;string e=Path.GetExtension(p).ToLowerInvariant();return e==".schematic"||e==".schem"||e==".litematic";}
 void ChooseWorldFolder(){string chosen="";
 #if UNITY_EDITOR
 chosen=EditorUtility.OpenFolderPanel("Minecraft-Welt auswählen",DefaultMinecraftSaves(),"");
 #elif UNITY_STANDALONE_OSX
 chosen=MacFolderPanel();
 #else
 status="Die Ordnerauswahl ist für diesen Build noch nicht verfügbar.";
 #endif
 if(String.IsNullOrEmpty(chosen))return;structurePath="";path=chosen;if(IsValidWorld(path)){saveName=Path.GetFileName(path);status="Minecraft-Welt erkannt. Du kannst sie jetzt importieren.";}else status="Dieser Ordner ist keine Minecraft-Java-Welt. Bitte den Weltordner mit level.dat und region auswählen.";}
 void ChooseStructureFile(){string chosen="";
 #if UNITY_EDITOR
 chosen=EditorUtility.OpenFilePanel("Schematic / Litematic auswählen",DefaultMinecraftSaves(),"");
 #elif UNITY_STANDALONE_OSX
 chosen=MacFilePanel();
 #else
 status="Die Dateiauswahl ist für diesen Build noch nicht verfügbar.";
 #endif
 if(String.IsNullOrEmpty(chosen))return;if(!IsValidStructure(chosen)){status="Bitte eine .schematic-, .schem- oder .litematic-Datei auswählen.";return;}path="";structurePath=chosen;saveName=Path.GetFileNameWithoutExtension(chosen);status="Bauwerk erkannt. Es wird in einer leeren Block-Island-Welt mittig platziert.";}
 string MacFilePanel(){try{string script="POSIX path of (choose file with prompt \"Schematic / Litematic auswählen\" of type {\"schematic\",\"schem\",\"litematic\"})";var psi=new ProcessStartInfo("/usr/bin/osascript"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};psi.ArgumentList.Add("-e");psi.ArgumentList.Add(script);using(var p=Process.Start(psi)){string s=p.StandardOutput.ReadToEnd().Trim();p.WaitForExit();return s;}}catch(Exception e){status="Dateiauswahl konnte nicht geöffnet werden: "+e.Message;return "";}}
 string DefaultMinecraftSaves(){string home=Environment.GetFolderPath(Environment.SpecialFolder.Personal);if(Application.platform==RuntimePlatform.OSXPlayer||Application.platform==RuntimePlatform.OSXEditor)return Path.Combine(home,"Library/Application Support/minecraft/saves");return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),".minecraft/saves");}
 string MacFolderPanel(){try{string start=DefaultMinecraftSaves();string script=Directory.Exists(start)?"set p to POSIX file \""+start.Replace("\\","\\\\").Replace("\"","\\\"")+"\"\nPOSIX path of (choose folder with prompt \"Minecraft-Welt auswählen\" default location p)":"POSIX path of (choose folder with prompt \"Minecraft-Welt auswählen\")";var psi=new ProcessStartInfo("/usr/bin/osascript"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};psi.ArgumentList.Add("-e");psi.ArgumentList.Add(script);using(var p=Process.Start(psi)){string s=p.StandardOutput.ReadToEnd().Trim();p.WaitForExit();return s.TrimEnd('/');}}catch(Exception e){status="Ordnerauswahl konnte nicht geöffnet werden: "+e.Message;return "";}}
 void DoImport(){busy=true;progress=new MinecraftWorldImporter.Progress();if(IsValidStructure(structurePath)){status="Schematic/Litematic wird gelesen …";StartCoroutine(MinecraftWorldImporter.ImportStructureAsync(structurePath,saveName,OnProgress,OnFinished));}else{status="Spawnpunkt wird gelesen und Import vorbereitet …";StartCoroutine(MinecraftWorldImporter.ImportAsync(path,saveName,radius,fullWorld,OnProgress,OnFinished));}}
 void OnProgress(MinecraftWorldImporter.Progress p){progress=p;status=IsValidStructure(structurePath)?"Bauwerk wird in Block-Island-Blöcke übersetzt …":"Schnellimport: Chunks werden sequenziell verarbeitet und direkt gespeichert …";}
 void OnFinished(MinecraftWorldImporter.Result res){status=res.message;if(res.ok){InfiniteWorldSave.RequestLoad(res.saveName);SceneManager.LoadScene("Game");return;}busy=false;}
}
