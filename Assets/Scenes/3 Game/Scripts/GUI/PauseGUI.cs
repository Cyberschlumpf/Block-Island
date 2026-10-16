using UnityEngine; using UnityEngine.SceneManagement;
public class PauseGUI:GUIScreen{
 int tab=0;string worldName="",saveStatus="";Vector2 scroll;
 void Awake(){worldName=string.IsNullOrEmpty(InfiniteWorldSave.CurrentWorldName)?"My World":InfiniteWorldSave.CurrentWorldName;VoxelBoxSettings.Apply();}
 void OnGUI(){VoxelBoxUI.BeginResponsive();VoxelBoxUI.Backdrop(.55f);Rect r=VoxelBoxUI.Safe(1040,840,30);GUILayout.BeginArea(r,VoxelBoxUI.Panel);GUILayout.Label("BLOCK ISLAND",VoxelBoxUI.Header);GUILayout.Label("PAUSE  •  "+(InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.BlockIsland?"BLOCK ISLAND WORLD":InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.Tanviir?"TANVIIR CREATIVE WORLD":"CREATIVE WORLD"),VoxelBoxUI.Small);GUILayout.Space(8);if(GUILayout.Button("▶  WEITERSPIELEN",VoxelBoxUI.Button,GUILayout.Height(44)))GameState.IsPlaying=true;GUILayout.Space(10);tab=VoxelBoxSettingsGUI.DrawTabs(tab);if(tab==6)BlockIslandGuides.Draw(34);else{scroll=GUILayout.BeginScrollView(scroll);if(tab==0||tab==2||tab==4||tab==5)VoxelBoxSettingsGUI.Draw(tab,true);else if(tab==1)DrawWorld();else if(tab==3)DrawAudio();GUILayout.EndScrollView();}GUILayout.Space(8);if(GUILayout.Button("←  HAUPTMENÜ",VoxelBoxUI.Danger,GUILayout.Height(42))){var w=Map.instance!=null?Map.instance.infiniteWorld:null;if(w!=null)InfiniteWorldSave.SaveCurrent(w);Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;SceneManager.LoadScene(0);}GUILayout.EndArea();VoxelBoxUI.EndResponsive();}
 void DrawWorld(){GUILayout.Label("WELT",VoxelBoxUI.Header);var d=InfiniteDayNightCycle.Instance;if(d!=null){GUILayout.Label("Tag & Nacht  •  "+d.ClockText()+"  (1 Tag = 60 Minuten)",VoxelBoxUI.Label);GUILayout.BeginHorizontal();if(GUILayout.Button(d.cycleEnabled?"ZYKLUS: EIN":"ZYKLUS: AUS",VoxelBoxUI.Button,GUILayout.Height(36)))d.SetCycle(!d.cycleEnabled);if(GUILayout.Button("NUR TAG (12:00)",VoxelBoxUI.Button,GUILayout.Height(36)))d.OnlyDay();GUILayout.EndHorizontal();float nv=VoxelBoxUI.Slider(d.timeOfDay,0,24);if(Mathf.Abs(nv-d.timeOfDay)>.001f)d.SetTime(nv,true);}
  var wth=InfiniteCreativeWeather.Instance;
  if(wth!=null){
   GUILayout.Space(10);GUILayout.Label("WETTER  •  jetzt: "+(VoxelBoxSettings.WeatherEffects?InfiniteCreativeWeather.DisplayName(wth.current)+(wth.RainbowVisible?"  •  Regenbogen":""):"aus"),VoxelBoxUI.Header);
   bool fx=VoxelBoxUI.Toggle(VoxelBoxSettings.WeatherEffects,"Wettereffekte anzeigen (Regen, Schnee, Gewitter, Wolken, Regenbogen)");if(fx!=VoxelBoxSettings.WeatherEffects)VoxelBoxSettings.WeatherEffects=fx;
   if(VoxelBoxSettings.WeatherEffects){
   bool auto=VoxelBoxUI.Toggle(wth.autoWeather,"Automatisches Zufallswetter");if(auto!=wth.autoWeather)wth.SetAuto(auto);
   GUILayout.Label("Oder ein Wetter fest einstellen:",VoxelBoxUI.Small);
   GUILayout.BeginHorizontal();
   foreach(InfiniteCreativeWeather.Kind k in System.Enum.GetValues(typeof(InfiniteCreativeWeather.Kind))){
    bool on=!wth.autoWeather&&wth.current==k;
    if(GUILayout.Button(InfiniteCreativeWeather.DisplayName(k),on?VoxelBoxUI.TabActive:VoxelBoxUI.Button,GUILayout.Height(36)))wth.SetWeather(k);
   }
   GUILayout.EndHorizontal();
   GUILayout.Space(4);
   int flakes=VoxelBoxSettings.BlizzardFlakes;
   GUILayout.Label("Schneesturm: bis zu "+flakes.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("de-DE"))+" liegende Flocken (Winterlandschaft)  –  auf eigene Gefahr, braucht viel Leistung",VoxelBoxUI.Small);
   int nf=Mathf.RoundToInt(VoxelBoxUI.Slider(flakes,VoxelBoxSettings.BlizzardFlakesMin,VoxelBoxSettings.BlizzardFlakesMax)/10000f)*10000;
   if(nf!=flakes)VoxelBoxSettings.BlizzardFlakes=nf;
   }
  }
  GUILayout.Space(14);GUILayout.Label("SPEICHERN",VoxelBoxUI.Header);worldName=GUILayout.TextField(worldName,48,GUILayout.Height(32));if(GUILayout.Button("▣  WELT SPEICHERN",VoxelBoxUI.Button,GUILayout.Height(38))){var w=Map.instance!=null?Map.instance.infiniteWorld:null;if(w!=null){worldName=InfiniteWorldSave.SanitizeName(worldName);saveStatus=InfiniteWorldSave.Save(w,worldName)?"Gespeichert: "+worldName:"Speichern fehlgeschlagen";if(InfiniteDayNightCycle.Instance!=null)InfiniteDayNightCycle.Instance.SavePrefs();}}
  if(InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.Tanviir&&GUILayout.Button("▣  TANVIIR SPEICHERN",VoxelBoxUI.Button,GUILayout.Height(38))){var w=Map.instance!=null?Map.instance.infiniteWorld:null;if(w!=null)saveStatus=InfiniteWorldSave.SaveTanviir(w)?"Tanviir gespeichert":"Tanviir speichern fehlgeschlagen";} if(InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.BlockIsland&&GUILayout.Button("▣  BLOCK ISLAND SPEICHERN",VoxelBoxUI.Button,GUILayout.Height(38))){var w=Map.instance!=null?Map.instance.infiniteWorld:null;if(w!=null)saveStatus=InfiniteWorldSave.SaveBlockIsland(w)?"Block Island gespeichert":"Block Island speichern fehlgeschlagen";}if(!string.IsNullOrEmpty(saveStatus))GUILayout.Label(saveStatus,VoxelBoxUI.Label);GUILayout.Label("Savegames: "+InfiniteWorldSave.SaveDirectory,VoxelBoxUI.Small);if(GUILayout.Button("▤  SAVEGAME-ORDNER ÖFFNEN",VoxelBoxUI.Button,GUILayout.Height(36)))InfiniteWorldSave.OpenSaveDirectory();DrawStructures();}
 // Saved structures: store the Copy & Paste clipboard under a name, load it in any world, then PASTE SELECTION.
 string structureName="Mein Bauwerk",structureStatus="";string[] structures;
 void DrawStructures(){
  GUILayout.Space(14);GUILayout.Label("BAUWERKE",VoxelBoxUI.Header);
  GUILayout.Label("Mit COPY START / COPY END kopieren, hier speichern – und in jeder Welt laden und mit PASTE SELECTION einsetzen.",VoxelBoxUI.Small);
  Builder b=FindAnyObjectByType<Builder>();
  VoxelClipboard clip=b!=null?b.CurrentClipboard:null;
  GUILayout.BeginHorizontal();
  structureName=GUILayout.TextField(structureName,48,GUILayout.Height(32));
  GUI.enabled=clip!=null&&!b.ClipboardBusy;
  if(GUILayout.Button("▣  KOPIE SPEICHERN",VoxelBoxUI.Button,GUILayout.Width(240),GUILayout.Height(34))){string msg;VoxelStructureLibrary.Save(clip,structureName,out msg);structureStatus=msg;structures=null;}
  GUI.enabled=true;GUILayout.EndHorizontal();
  if(clip==null)GUILayout.Label("Zwischenablage leer – erst etwas kopieren.",VoxelBoxUI.Small);
  else GUILayout.Label("Zwischenablage: "+clip.sizeX+" x "+clip.sizeY+" x "+clip.sizeZ+", "+clip.solidCount.ToString("N0")+" Blöcke",VoxelBoxUI.Small);
  if(structures==null)structures=VoxelStructureLibrary.List();
  if(structures.Length==0)GUILayout.Label("Noch keine Bauwerke gespeichert.",VoxelBoxUI.Label);
  foreach(string n in structures){
   GUILayout.BeginHorizontal(VoxelBoxUI.Card);GUILayout.Label(n,VoxelBoxUI.Label);GUILayout.FlexibleSpace();
   GUI.enabled=b!=null&&!b.ClipboardBusy;
   if(GUILayout.Button("▶  LADEN",VoxelBoxUI.Button,GUILayout.Width(160),GUILayout.Height(32))){string msg;VoxelClipboard c=VoxelStructureLibrary.Load(n,out msg);if(c!=null)b.SetClipboard(c,msg);structureStatus=msg;}
   GUI.enabled=true;GUILayout.EndHorizontal();
  }
  if(!string.IsNullOrEmpty(structureStatus))GUILayout.Label(structureStatus,VoxelBoxUI.Label);
  GUILayout.BeginHorizontal();
  if(GUILayout.Button("↻  LISTE AKTUALISIEREN",VoxelBoxUI.Button,GUILayout.Height(34)))structures=null;
  if(GUILayout.Button("▤  BAUWERKE-ORDNER ÖFFNEN",VoxelBoxUI.Button,GUILayout.Height(34)))VoxelStructureLibrary.OpenFolder();
  GUILayout.EndHorizontal();
 }
 void DrawAudio(){GUILayout.Label("AUDIO",VoxelBoxUI.Header);var m=InfiniteMusicPlayer.Instance;if(m==null){GUILayout.Label("Musikplayer wird initialisiert ...",VoxelBoxUI.Label);return;}GUILayout.BeginHorizontal();if(GUILayout.Button("MP3-ORDNER WÄHLEN",VoxelBoxUI.Button,GUILayout.Height(38)))m.ChooseFolder();GUILayout.Label(string.IsNullOrEmpty(m.folderPath)?"Kein Ordner":m.folderPath,VoxelBoxUI.Small);GUILayout.EndHorizontal();GUILayout.Label("Playlist: "+m.tracks.Count+" / "+InfiniteMusicPlayer.MaxTracks+"   •   "+m.CurrentTitle,VoxelBoxUI.Label);GUILayout.BeginHorizontal();if(GUILayout.Button("|<",VoxelBoxUI.Button))m.Previous();if(GUILayout.Button("PLAY",VoxelBoxUI.Button))m.Play();if(GUILayout.Button("PAUSE",VoxelBoxUI.Button))m.Pause();if(GUILayout.Button("STOP",VoxelBoxUI.Button))m.Stop();if(GUILayout.Button(">|",VoxelBoxUI.Button))m.Next();if(GUILayout.Button(m.shuffle?"SHUFFLE: EIN":"SHUFFLE: AUS",VoxelBoxUI.Button))m.ToggleShuffle();GUILayout.EndHorizontal();GUILayout.Label("Lautstärke  "+Mathf.RoundToInt(m.volume*100)+"%",VoxelBoxUI.Label);float v=VoxelBoxUI.Slider(m.volume,0,1);if(Mathf.Abs(v-m.volume)>.001f)m.SetVolume(v);}
 void Update(){if(GameState.IsPlaying)SetScreen<GameGUI>();}
}
