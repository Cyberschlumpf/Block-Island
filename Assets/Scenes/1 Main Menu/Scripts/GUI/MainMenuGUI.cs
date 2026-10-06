using UnityEngine; using UnityEngine.SceneManagement;
public class MainMenuGUI:GUIScreen{
 enum SidePanel{None,Settings,Controls,Guides,BlockDesigner} SidePanel side=SidePanel.None; int tab=0, guideTab=0; Vector2 guideScroll=Vector2.zero;
 void Awake(){RestoreMenuCursor();VoxelBoxSettings.Apply();} void OnEnable(){RestoreMenuCursor();side=SidePanel.None;} void RestoreMenuCursor(){Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
 void OnGUI(){VoxelBoxUI.BeginResponsive();VoxelBoxUI.MenuBackground();DrawMain();if(side==SidePanel.Settings)DrawSettings();else if(side==SidePanel.Controls)DrawControlsPanel();else if(side==SidePanel.Guides)DrawGuidesPanel();else if(side==SidePanel.BlockDesigner)DrawBlockDesignerPanel();VoxelBoxUI.EndResponsive();}
 void OpenMinecraftImport(){ var importer=GetComponent<MinecraftImportGUI>(); if(importer==null) importer=gameObject.AddComponent<MinecraftImportGUI>(); foreach(GUIScreen screen in GetComponents<GUIScreen>()) screen.enabled=(screen==importer); }
 void DrawMain(){
  // 0.9.6: real visible controls over the artwork. No invisible hit targets.
  Rect r=new Rect(24,292,370,584);
  GUILayout.BeginArea(r);
  GUILayout.BeginVertical(VoxelBoxUI.Panel);
  if(VoxelBoxUI.MenuButton("◆","NEUE WELT","")){InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.Islands);SceneManager.LoadScene("Map Generator");}
  if(VoxelBoxUI.MenuButton("▶","WELT LADEN",""))SetScreen<LoadMapGUI>();
  if(VoxelBoxUI.MenuButton("▧","MINECRAFT IMPORT",""))OpenMinecraftImport();
  if(VoxelBoxUI.MenuButton("✦","TANVIIR","")){InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.Tanviir);SceneManager.LoadScene("Game");}
  if(VoxelBoxUI.MenuButton("▣","BLOCK ISLAND","")){InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.BlockIsland);SceneManager.LoadScene("Game");}
  if(VoxelBoxUI.MenuButton("☁","SKY ISLANDS","")){InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.Islands);InfiniteWorldLaunchConfig.openSkyGenerator=true;SceneManager.LoadScene("Map Generator");}
  if(VoxelBoxUI.MenuButton("✺","LICHTGARTEN","")){InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.LightGarden);SceneManager.LoadScene("Game");}
  if(VoxelBoxUI.MenuButton("∞","SINGULARITÄT","")){InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.Relativity);SceneManager.LoadScene("Game");}
  if(VoxelBoxUI.MenuButton("⚙","EINSTELLUNGEN","")){tab=0;side=SidePanel.Settings;}
  if(VoxelBoxUI.MenuButton("▦","BLOCK-DESIGNER","")){side=SidePanel.BlockDesigner;}
  GUILayout.FlexibleSpace();
  if(GUILayout.Button("✕  SPIEL BEENDEN",VoxelBoxUI.Danger,GUILayout.Height(46)))Application.Quit();
  GUILayout.EndVertical();GUILayout.EndArea();
 }
 void DrawSettings(){VoxelBoxUI.Backdrop(.08f);Rect r=new Rect(500,34,1062,832);GUILayout.BeginArea(r,VoxelBoxUI.Panel);VoxelBoxUI.PanelTitle("⚙","EINSTELLUNGEN","BLOCK ISLAND  •  SYSTEM, SPIEL & ANLEITUNGEN");int oldTab=tab;tab=VoxelBoxSettingsGUI.DrawTabs(tab);if(tab!=oldTab&&tab==6){guideTab=0;guideScroll=Vector2.zero;}GUILayout.Space(8);if(tab==6)DrawGuidesInSettings();else{GUILayout.BeginVertical(VoxelBoxUI.Card);VoxelBoxSettingsGUI.Draw(tab,false);GUILayout.EndVertical();GUILayout.FlexibleSpace();}CloseRow();GUILayout.EndArea();}
 void DrawGuidesInSettings(){
  string[] tabs={"TANVIIR","GESCHICHTE","BLOCKAUSWAHL","WORKSHOP","COPY & PASTE","TNT","MINECART","SPEICHERN","WETTER","AUDIO","TIPPS"};
  for(int row=0;row<3;row++){GUILayout.BeginHorizontal();int start=row*4;int end=Mathf.Min(start+4,tabs.Length);for(int i=start;i<end;i++){if(GUILayout.Button(tabs[i],guideTab==i?VoxelBoxUI.TabActive:VoxelBoxUI.Tab,GUILayout.Height(36))){guideTab=i;guideScroll=Vector2.zero;}}GUILayout.EndHorizontal();}
  GUILayout.Space(6);guideScroll=GUILayout.BeginScrollView(guideScroll,false,true,GUILayout.ExpandHeight(true));GUILayout.BeginVertical(VoxelBoxUI.Card);
  if(guideTab==0)DrawGuideTanviir();else if(guideTab==1)DrawGuideHistory();else if(guideTab==2)DrawGuideBlockSelection();else if(guideTab==3)DrawGuideWorkshop();else if(guideTab==4)DrawGuideCopyPaste();else if(guideTab==5)DrawGuideTNT();else if(guideTab==6)DrawGuideMinecart();else if(guideTab==7)DrawGuideSaveLoad();else if(guideTab==8)DrawGuideWeather();else if(guideTab==9)DrawGuideAudio();else DrawGuideTips();
  GUILayout.EndVertical();GUILayout.EndScrollView();GUILayout.Space(6);
 }
 void DrawControlsPanel(){
  VoxelBoxUI.Backdrop(.12f);Rect r=new Rect(520,44,1042,812);GUILayout.BeginArea(r,VoxelBoxUI.Panel);
  VoxelBoxUI.PanelTitle("⌨","STEUERUNG","TASTATUR & MAUS  •  CREATIVE MODE");GUILayout.Space(8);GUILayout.BeginHorizontal();
  GUILayout.BeginVertical(VoxelBoxUI.Card,GUILayout.Width(485));VoxelBoxUI.SectionTitle("MAUS & BEWEGUNG");
  GUILayout.Label("Linksklick     Block setzen / Werkzeug benutzen\nRechtsklick    Block entfernen\nMausrad        Blockauswahl wechseln\n\nW A S D        Bewegen\nSPACE          Aufwärts fliegen\nSHIFT          Abwärts fliegen\nF              Flugmodus ein/aus\nESC            Pause / Menü",VoxelBoxUI.Label);GUILayout.EndVertical();GUILayout.Space(12);
  GUILayout.BeginVertical(VoxelBoxUI.Card,GUILayout.Width(485));VoxelBoxUI.SectionTitle("CREATIVE TOOLS");
  GUILayout.Label("E              Block-Auswahl\nH              PNG-Screenshot\nM              Minecart ein-/aussteigen\n\nCOPY START     Erste Ecke markieren\nCOPY END       Zweite Ecke markieren\nPASTE          Auswahl einsetzen\nR bei PASTE    Auswahl +90° drehen (0°/90°/180°/270°)\nTNT            Creative-Sprengwerkzeug\nWORKSHOP       8×8×8 Custom Block",VoxelBoxUI.Label);GUILayout.EndVertical();GUILayout.EndHorizontal();
  GUILayout.Space(12);GUILayout.BeginVertical(VoxelBoxUI.Card);VoxelBoxUI.SectionTitle("EINSTELLUNGEN");GUILayout.Label("Maus-Empfindlichkeit und weitere Eingabeoptionen findest du unter Einstellungen → Steuerung.",VoxelBoxUI.Label);GUILayout.EndVertical();
  GUILayout.FlexibleSpace();CloseRow();GUILayout.EndArea();
 }

 void DrawGuidesPanel(){
  VoxelBoxUI.Backdrop(.12f);Rect r=new Rect(500,34,1062,832);GUILayout.BeginArea(r,VoxelBoxUI.Panel);
  VoxelBoxUI.PanelTitle("?","ANLEITUNGEN, HILFEN & TIPPS","BLOCK ISLAND  •  KURZ ERKLÄRT");
  string[] tabs={"TANVIIR","GESCHICHTE","BLOCKAUSWAHL","WORKSHOP","COPY & PASTE","TNT","MINECART","SPEICHERN","WETTER","AUDIO","TIPPS"};
  for(int row=0;row<3;row++){GUILayout.BeginHorizontal();int start=row*4;int end=Mathf.Min(start+4,tabs.Length);for(int i=start;i<end;i++){if(GUILayout.Button(tabs[i],guideTab==i?VoxelBoxUI.TabActive:VoxelBoxUI.Tab,GUILayout.Height(38))){guideTab=i;guideScroll=Vector2.zero;}}GUILayout.EndHorizontal();}
  GUILayout.Space(8);guideScroll=GUILayout.BeginScrollView(guideScroll,false,true,GUILayout.ExpandHeight(true));GUILayout.BeginVertical(VoxelBoxUI.Card);
  if(guideTab==0)DrawGuideTanviir();else if(guideTab==1)DrawGuideHistory();else if(guideTab==2)DrawGuideBlockSelection();else if(guideTab==3)DrawGuideWorkshop();else if(guideTab==4)DrawGuideCopyPaste();else if(guideTab==5)DrawGuideTNT();else if(guideTab==6)DrawGuideMinecart();else if(guideTab==7)DrawGuideSaveLoad();else if(guideTab==8)DrawGuideWeather();else if(guideTab==9)DrawGuideAudio();else DrawGuideTips();
  GUILayout.EndVertical();GUILayout.EndScrollView();GUILayout.Space(8);CloseRow();GUILayout.EndArea();
 }
 void DrawBlockDesignerPanel(){VoxelBoxUI.Backdrop(.12f);Rect r=new Rect(370,28,1192,844);GUILayout.BeginArea(r,VoxelBoxUI.Panel);VoxelBoxUI.PanelTitle("▦","BLOCK-DESIGNER","BLOCK-ID • GRUNDSTRUKTUR • GLOBALE TEXTUR");BlockDesignerGUI.Draw();GUILayout.FlexibleSpace();CloseRow();GUILayout.EndArea();}
 void GuideHeading(string t){VoxelBoxUI.SectionTitle(t);}
 void GuideText(string t){GUILayout.Label(t,VoxelBoxUI.Label);GUILayout.Space(9);}
 void DrawGuideTanviir(){
  GuideHeading("TANVIIR – EINE WELT DER HIGH FANTASY");
  GuideText("Tanviir ist eine historische Fantasywelt aus den frühen Jahren von Minecraft. Sie wurde von Mitgliedern der Bau-Community The VoxelBox als zusammenhängende High-Fantasy-Region entwickelt. Landschaft, Vegetation, Wasser und Architektur wurden dabei bewusst gemeinsam gestaltet. Zu den überlieferten Orten gehört unter anderem Anataria.");
  GuideText("Spätestens 2013 war Tanviir als eigene kuratierte Region dokumentiert. Am 1. April 2014 veröffentlichte The VoxelBox die vollständige Welt unter dem Titel „Tanviir, a World of High Fantasy“. Als Lead Architects wurden JiiJiii, Thimble_Tack und Daniel_Carmi genannt; zahlreiche weitere Mitglieder der Community wirkten ebenfalls an der Region mit.");
  GuideText("Für Tanviir entstand außerdem ein eigenes 32×32 Texture Pack. Es unterstützte die typische Gestaltung der Welt mit hellen, monumentalen Bauwerken, Fantasy-Architektur und stark ausgearbeiteten Landschaften. Teile der damaligen Dokumentation und des VoxelWiki sind heute nicht mehr verfügbar, weshalb nicht mehr alle Orte und Erbauer vollständig zugeordnet werden können.");
  GuideText("In Block Island wird Tanviir als feste Welt erhalten. Die historischen Weltdaten bilden die Grundlage, können aber mit der heutigen Engine weiter bebaut, verändert und gespeichert werden. Dadurch bleibt die ursprüngliche Welt nutzbar, ohne weiterhin von Minecraft als Laufzeitumgebung abhängig zu sein.");
 }
 void DrawGuideBlockSelection(){
  GuideHeading("BLOCKAUSWAHL & EIGENE KATEGORIEN");
  GuideText("Mit E öffnest du die Block-Auswahl. Die Blöcke sind in BLÖCKE, NATUR, FUNKTION, CUSTOM und FAVORITEN gegliedert. Ein kurzer Linksklick auf ein Symbol wählt den Block wie gewohnt aus.");
  GuideText("Möchtest du einen Block selbst einsortieren, halte sein Symbol mit der linken Maustaste mindestens 3 Sekunden gedrückt. Danach öffnet sich BLOCK SORTIEREN. Dort kannst du den Block nach BLÖCKE, NATUR, FUNKTION oder CUSTOM verschieben.");
  GuideText("FAVORITEN ist eine zusätzliche persönliche Sammlung: Über BLOCK SORTIEREN kannst du einen Block zu FAVORITEN hinzufügen oder wieder daraus entfernen. Der Block bleibt gleichzeitig in seiner Hauptkategorie erhalten. Favoriten werden mit einem Stern markiert.");
  GuideText("Deine persönlichen Kategorien und Favoriten werden gespeichert und stehen nach einem Neustart wieder zur Verfügung. Die eigentlichen Block-IDs und die Welt werden durch das Umsortieren nicht verändert.");
 }
 void DrawGuideWorkshop(){
  GuideHeading("CUSTOM BLOCK WORKSHOP");
  GuideText("Mit dem Workshop baust du aus einer 8×8×8 großen Arbeitsbox einen eigenen Block. Wähle zuerst den Block „Custom Workshop 8x8“ und setze die Box an einer freien Stelle. Baue anschließend innerhalb der markierten Box dein Modell. Alle dort gesetzten Mini-Voxel werden beim Backen zu einem einzelnen 1×1×1 Custom Block zusammengefasst.");
  GuideText("Im Workshop wählst du einen der Custom-Slots 01–32. Mit TAB wechselst du in den UI-Modus. Die Kamera bleibt dabei stehen und der Mauszeiger wird freigegeben. Nun kannst du Bewegungsanimation, Geschwindigkeit, Partikeleffekt und Partikelstärke einstellen. Die Live-Vorschau neben der Box zeigt das Ergebnis bereits vor dem Backen. Mit einem weiteren TAB kehrst du in den Baumodus zurück.");
  GuideText("Wenn Modell und Effekte passen, wähle „JETZT BACKEN“. Danach findest du den fertigen Custom Block im BlockSet und kannst ihn wie einen normalen Block setzen. Animation und Partikeleffekt gehören zum jeweiligen Custom-Slot. Wird ein Slot später neu gebacken, erhält dieser Slot das neue Modell und die neuen Einstellungen.");
  GuideText("Tipp: Für ruhige Bauteile eignet sich „None“. Pflanzen lassen sich zum Beispiel mit „Wind“ und „Glowing Dust“ kombinieren, technische Objekte mit „Rotate“ oder „Piston“ und Funken, und dekorative Objekte mit „Hover“, „Magic“ oder „Sparkles“.");
 }
 void DrawGuideCopyPaste(){
  GuideHeading("COPY & PASTE");
  GuideText("Copy & Paste kopiert einen rechteckigen Bereich der Welt. Wähle zuerst „Copy Start“ und klicke direkt auf den Block an der ersten Ecke des gewünschten Bereichs. Wähle danach „Copy End“ und klicke auf den Block an der gegenüberliegenden Ecke. Die Auswahl wird markiert und in den Kopierpuffer übernommen.");
  GuideText("Wähle anschließend „Paste Selection“. Die transparente Vorschau zeigt, wo die Kopie eingesetzt wird. Mit R drehst du die Kopie vor dem Einsetzen jeweils um 90° (0° → 90° → 180° → 270° → 0°). Alternativ: Pfeil rechts/links = ±90°, Pfeil hoch = 180°, Pfeil runter = 0°. Setze sie danach an der gewünschten Position. Kopiert werden die vollständigen Blockdaten innerhalb des markierten Bereichs, einschließlich leerer Stellen. Dadurch bleiben auch Hohlräume und Zwischenräume eines Bauwerks erhalten.");
  GuideText("Eine Auswahl darf maximal 32×32×32 Blöcke groß sein. Enthält die Auswahl überhaupt keinen gesetzten Block, wird sie nicht übernommen. Der Kopierpuffer gilt für die aktuelle Spielsitzung.");
 }
 void DrawGuideTNT(){
  GuideHeading("CREATIVE TNT");
  GuideText("Creative TNT ist ein Werkzeug zum schnellen Bearbeiten großer Bereiche. Wähle „Creative TNT“ im BlockSet und setze es an der Stelle, die das Zentrum der Entfernung bilden soll. Danach öffnet sich die Radiusauswahl.");
  GuideText("Der Radius kann zwischen 8 und 32 Blöcken eingestellt werden. Mit „ZÜNDEN“ beginnt ein Countdown von drei Sekunden. Anschließend werden die Blöcke innerhalb eines kugelförmigen Bereichs entfernt. Die Änderung wird wie andere Weltbearbeitungen gespeichert.");
  GuideText("Creative TNT verursacht keinen Spielerschaden und ist keine Waffenmechanik. Es ist für Terraforming, das Entfernen großer Bauabschnitte und schnelle Umbauten gedacht. Vor großen Änderungen empfiehlt es sich, die Welt zu speichern.");
 }
 void DrawGuideHistory(){
  GuideHeading("DIE GESCHICHTE VON BLOCK ISLAND");
  GuideText("Block Island entstand zwischen 2014 und 2019 als privates Voxel-Spielprojekt. Zu Beginn gab es weder besondere Programmierkenntnisse noch einen fertigen Entwicklungsplan. Viele Funktionen entstanden durch Ausprobieren, Umbauen und wiederholtes Testen. So entwickelten sich nach und nach eigene Welten, Gebäude und Spielmechaniken.");
  GuideText("2026 wurde das alte Projekt wieder aufgegriffen. Mit Unterstützung moderner KI-Werkzeuge konnten alte Daten analysiert, frühere Welten wiederhergestellt und technische Probleme der ursprünglichen Version schrittweise gelöst werden.");
  GuideText("Aus der Wiederbelebung von Block Island entstand die heutige Fassung von Block Island. Sie verbindet die restaurierte ursprüngliche Welt mit der weiterentwickelten Voxel-Engine, neuen Creative-Werkzeugen, dem Custom Block Workshop und weiteren festen Welten wie Tanviir. Die historische Block-Island-Welt bleibt dabei erhalten und kann weiterhin verändert und gespeichert werden.");
 }
 void DrawGuideMinecart(){
  GuideHeading("MINECART");
  GuideText("Lege zuerst eine zusammenhängende Strecke aus den Minecart-Schienen an und platziere anschließend ein Minecart auf der Strecke. Die Schienen unterstützen gerade Abschnitte, Kurven sowie Höhenwechsel der Strecke.");
  GuideText("Gehe in die Nähe des Minecarts und drücke M, um einzusteigen. Während der Fahrt folgt der Spieler dem Minecart; die Kamera kann weiter benutzt werden. Drücke erneut M, um auszusteigen. Der Spieler wird neben dem Minecart abgesetzt.");
 }
 void DrawGuideSaveLoad(){
  GuideHeading("SPEICHERN & LADEN");
  GuideText("Im Pause-Menü unter Welt kann der aktuelle Stand manuell gespeichert werden. Tanviir und Block Island besitzen zusätzlich eigene Speicherfunktionen. Während des Spiels wird außerdem regelmäßig automatisch gespeichert.");
  GuideText("Die Savegames liegen im Ordner Dokumente/Block Island/Saves. Über „Savegame-Ordner öffnen“ kann dieser Ordner direkt aus dem Spiel geöffnet werden. Im Hauptmenü lädt „Welt laden“ einen vorhandenen Spielstand.");
  GuideText("Bei Tanviir und Block Island bleibt die feste Ausgangswelt unverändert. Gespeichert werden die Änderungen, die während des Spielens vorgenommen wurden. Dadurch kann jederzeit weitergebaut werden, ohne die historische Basiswelt neu schreiben zu müssen.");
 }
 void DrawGuideWeather(){
  GuideHeading("WETTER");
  GuideText("Die Wettersteuerung befindet sich im Pause-Menü unter Welt. Regen und Nebel können unabhängig voneinander ein- oder ausgeschaltet werden. „Klares Wetter“ schaltet beide Effekte wieder aus.");
  GuideText("Die Wettereinstellungen werden gespeichert. Sie verändern nur die Darstellung und Atmosphäre der Welt und haben keinen Einfluss auf Schaden, Hunger oder andere Survival-Mechaniken.");
 }
 void DrawGuideAudio(){
  GuideHeading("AUDIO-PLAYER");
  GuideText("Der Musikplayer befindet sich im Pause-Menü unter Audio. Über „MP3-Ordner wählen“ wird ein eigener Musikordner ausgewählt. Die gefundenen Titel werden als Playlist geladen.");
  GuideText("Mit Zurück, Play, Pause, Stop und Weiter wird die Wiedergabe gesteuert. Shuffle schaltet die zufällige Titelreihenfolge ein oder aus. Die Lautstärke kann unabhängig über den Regler eingestellt werden. Der aktuell abgespielte Titel wird im Audio-Bereich angezeigt.");
 }
 void DrawGuideTips(){
  GuideHeading("KURZE HILFEN & TIPPS");
  GuideText("E öffnet die Block-Auswahl. Mit dem Mausrad wechselst du durch die Auswahl. Linksklick setzt einen Block bzw. benutzt das ausgewählte Werkzeug, Rechtsklick entfernt einen Block. H speichert einen PNG-Screenshot. M steigt bei einem Minecart ein oder aus.");
  GuideText("Im Custom Workshop schaltet TAB zwischen Baumodus und UI-Bedienung um. ESC öffnet weiterhin das Pause-Menü. Für größere Umbauten können Copy & Paste und Creative TNT kombiniert werden. Speichere wichtige Zwischenstände, bevor du große Bereiche veränderst.");
 }

 void CloseRow(){GUILayout.BeginHorizontal();GUILayout.FlexibleSpace();if(GUILayout.Button("SCHLIESSEN  ✕",VoxelBoxUI.Danger,GUILayout.Width(220),GUILayout.Height(48)))side=SidePanel.None;GUILayout.EndHorizontal();}
}
