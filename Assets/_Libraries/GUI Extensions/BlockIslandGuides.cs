using UnityEngine;

// The guide pages ("Anleitungen"), shared by the main menu (Einstellungen → Anleitungen) and the pause menu.
public static class BlockIslandGuides {
 static int guideTab; static Vector2 guideScroll;
 static readonly string[] tabs={"TASTEN & PORTALE","FUNKTIONSBLÖCKE","GESCHICHTE","BLOCKAUSWAHL","WORKSHOP","COPY & PASTE","TNT","MINECART","SPEICHERN","WETTER","AUDIO","TIPPS"};
 public static void Reset(){guideTab=0;guideScroll=Vector2.zero;}
 // tab buttons (3 rows) and the scrollable page below them
 public static void Draw(float tabHeight){
  for(int row=0;row<3;row++){GUILayout.BeginHorizontal();int start=row*4;int end=Mathf.Min(start+4,tabs.Length);for(int i=start;i<end;i++){if(GUILayout.Button(tabs[i],guideTab==i?VoxelBoxUI.TabActive:VoxelBoxUI.Tab,GUILayout.Height(tabHeight))){guideTab=i;guideScroll=Vector2.zero;}}GUILayout.EndHorizontal();}
  GUILayout.Space(6);guideScroll=GUILayout.BeginScrollView(guideScroll,false,true,GUILayout.ExpandHeight(true));GUILayout.BeginVertical(VoxelBoxUI.Card);
  if(guideTab==0)DrawGuideKeysPortals();else if(guideTab==1)DrawGuideFunctionBlocks();else if(guideTab==2)DrawGuideHistory();else if(guideTab==3)DrawGuideBlockSelection();else if(guideTab==4)DrawGuideWorkshop();else if(guideTab==5)DrawGuideCopyPaste();else if(guideTab==6)DrawGuideTNT();else if(guideTab==7)DrawGuideMinecart();else if(guideTab==8)DrawGuideSaveLoad();else if(guideTab==9)DrawGuideWeather();else if(guideTab==10)DrawGuideAudio();else DrawGuideTips();
  GUILayout.EndVertical();GUILayout.EndScrollView();
 }
  static void GuideHeading(string t){VoxelBoxUI.SectionTitle(t);}
  static void GuideText(string t){GUILayout.Label(t,VoxelBoxUI.Label);GUILayout.Space(9);}
  static void DrawGuideKeysPortals(){
  GuideHeading("TASTE T – ZURÜCK ZUM STARTPUNKT");
  GuideText("Mit T kehrst du jederzeit zum Startpunkt der aktuellen Welt zurück, zum Beispiel wenn du dich verlaufen hast oder weit hinaus aufs Meer geflogen bist. Die Taste wirkt im Spiel, solange kein Menü geöffnet ist.");
  GuideHeading("TASTE J – HÖHE VON FIGUREN UND OBJEKTEN");
  GuideText("Schau auf eine Figur oder ein Objekt aus Block Island 1.0 (bis etwa 16 Blöcke entfernt) und drücke J. Im Menü stellst du ein, wie hoch das Modell in seinem Block steht – von einem Block unter dem Boden bis zur Oberkante des Blocks. Die Höhe gilt für alle gesetzten Kopien dieses Blocks und wird gespeichert. „Automatisch“ stellt die gemessene Standhöhe wieder her. Ein weiteres J schließt das Menü.");
  GuideHeading("TASTE P – PREFAB-IMPORTER");
  GuideText("Mit P öffnest du den Prefab-Importer. Er zeigt alle Prefabs, die du in Unity als .biprefab in den Ordner Dokumente/Block Island/Prefabs exportiert hast. In der drehbaren 3D-Vorschau legst du Größe, Animation und Höhe fest und speicherst das Prefab unter einem Namen als eigenen Block. Danach findest du ihn in der Block-Auswahl und kannst ihn bauen. Gespeicherte Blöcke lassen sich hier später wieder ändern – alle bereits gesetzten Kopien übernehmen die Änderung.");
  GuideHeading("PORTALE");
  GuideText("In der Block-Auswahl unter FUNKTION findest du „Portal → Block Island“ und „Portal → Tanviir“. Ein gesetztes Portal wird zu einem leuchtenden Würfel aus 4×4×4 Blöcken. Stelle es auf eine freie, ebene Fläche und halte rund um den gesetzten Block etwa zwei Blöcke frei.");
  GuideText("Die Seiten des Würfels zeigen einen Rundum-Blick der Zielwelt – wie ein Fenster, durch das du hindurchschaust, wenn du dich davor bewegst. Solange du die Zielwelt noch nie besucht hast, zeigt das Portal einen Sternennebel. Das Bild wird bei jedem Besuch erneuert.");
  GuideText("Gehe in den Würfel, um zu reisen: Der Bildschirm wird langsam dunkel, Sterne bilden sich, deine Welt wird gespeichert und die andere geladen. Danach lösen sich die Sterne auf und du stehst vor dem Portal der anderen Welt, mit derselben Blickrichtung wie beim Hineingehen. Gibt es dort noch kein Portal, landest du an deinem zuletzt gespeicherten Platz.");
  GuideText("Tanviir ist eine verborgene Stadt. Ein Portal ist der Weg dorthin – und mit einem Portal nach Block Island kommst du wieder zurück. Portale werden wie normale Blöcke abgebaut.");
 }
  static void DrawGuideFunctionBlocks(){
  GuideHeading("FUNKTIONSBLÖCKE – ALLGEMEIN");
  GuideText("Alle Funktionsblöcke findest du in der Block-Auswahl unter FUNKTION. Es sind echte, feste Blöcke: Du kannst darauf stehen und daran weiterbauen. Mit ENTER benutzt du den Block, auf den das Fadenkreuz zeigt – zum Beispiel Glocke läuten, Text ändern, Weiche umstellen oder die Helligkeit einer Laterne einstellen. Texte und Einstellungen werden für jede Welt gespeichert.");
  GuideHeading("ECHTE WELT");
  GuideText("Infotafel Block Island: zwei Blöcke breit, zeigt die Uhrzeit auf der echten Insel Block Island (Rhode Island, USA) sowie Wetter und Temperatur dort. Die Daten kommen alle 15 Minuten aus dem Internet.");
  GuideText("Wetterstation: Das Windrad dreht sich mit dem echten Wind auf Block Island, die Wetterfahne zeigt, woher der Wind kommt (Norden liegt in Richtung +Z), dazu die Temperatur.");
  GuideText("Weltzeituhren: Berlin, New York, Tokio und Sydney – jeweils mit Sommer- und Winterzeit.  Mondphase: zeigt den Mond von heute mit Namen und beleuchtetem Anteil.  Gezeitentafel Block Island: nächstes Hoch- und Niedrigwasser der echten Insel und ob das Wasser gerade steigt oder fällt.");
  GuideHeading("LICHT & STIMMUNG");
  GuideText("Laterne (Abendlicht) und die alte Straßenlaterne aus Block Island gehen in der Dämmerung, nachts und bei Regen, Gewitter oder Schneesturm von selbst an – nicht alle gleichzeitig, sondern nacheinander wie in einer echten Straße. ENTER auf eine Laterne stellt die Helligkeit ein: Aus, Schwach, Mittel, Hell oder Sehr hell.");
  GuideText("Leuchtfeuer: die Laterne für die Spitze eines Leuchtturms. Bei Dämmerung, Nacht und schlechtem Wetter dreht sich ein heller Lichtkegel über das Meer.  Feuerwerk: ENTER startet eine Salve, um Mitternacht gibt es von selbst ein großes Feuerwerk.");
  GuideHeading("BEWEGUNG & TECHNIK");
  GuideText("Teleporter: Beim Setzen gibst du einen Namen ein. Alle Teleporter einer Welt mit demselben Namen sind verbunden – stell dich kurz darauf und du wirst zum nächsten gebracht. Bei drei oder mehr geht es reihum. ENTER benennt um.");
  GuideText("Aufzug: Setze zwei oder mehr Aufzug-Blöcke senkrecht übereinander (eine Etage pro Block). Stell dich kurz darauf und er fährt dich zur nächsten Etage nach oben, vom obersten Stockwerk wieder ganz nach unten.");
  GuideText("Förderband: trägt dich in die Richtung, in die du beim Setzen geschaut hast.  Sprungfeder: schleudert dich etwa fünf Blöcke hoch.  Tür (automatisch): zwei Blöcke hoch, öffnet sich, wenn du näherkommst, und schließt hinter dir.");
  GuideText("Minecart-Bahnhof: neben eine Schiene gesetzt, hält dort jedes Minecart einige Sekunden.  Weiche: neben eine Abzweigung gesetzt; ENTER schaltet zwischen „geradeaus“ (grün) und „abbiegen“ (orange).");
  GuideHeading("KLANG");
  GuideText("Glocke: schlägt zur vollen Stunde der echten Uhrzeit (1 bis 12 Schläge), ENTER läutet sie einmal.  Musikbox: spielt die MP3s aus deinem Musikordner im Umkreis ab, ENTER startet bzw. springt zum nächsten Titel.  Naturklang: Vogelgezwitscher am Tag und Grillen in der Nacht; ENTER wählt Vögel, Grillen, Wasser, Wind oder Aus.");
  GuideHeading("BAUEN & ORDNUNG");
  GuideText("Schild mit Text: Beim Setzen gibst du den Text ein (bis zu drei Zeilen, automatisch umbrochen), ENTER ändert ihn.  Wegpunkt: eine Fahne mit Namen. Mit der Taste N siehst du alle Wegpunkte der Welt und reist mit einem Klick dorthin.");
  GuideText("Bauwerk-Spawner: Beim Setzen erscheint die Liste deiner gespeicherten Bauwerke; das gewählte wird genau an dieser Stelle gebaut.  Frost-Brunnen: Fontäne, die zu Eis gefriert, wenn es auf der echten Insel Block Island friert.  Info-Säule: drei Blöcke hoch, dreht sich langsam und zeigt nacheinander die Bilder aus dem Ordner Dokumente/Block Island/Bilder (PNG oder JPG).");
 }
  static void DrawGuideBlockSelection(){
  GuideHeading("BLOCKAUSWAHL & EIGENE KATEGORIEN");
  GuideText("Mit E öffnest du die Block-Auswahl. Die Blöcke sind in BLÖCKE, NATUR, FUNKTION, CUSTOM und FAVORITEN gegliedert. Ein kurzer Linksklick auf ein Symbol wählt den Block wie gewohnt aus.");
  GuideText("Möchtest du einen Block selbst einsortieren, halte sein Symbol mit der linken Maustaste mindestens 3 Sekunden gedrückt. Danach öffnet sich BLOCK SORTIEREN. Dort kannst du den Block nach BLÖCKE, NATUR, FUNKTION oder CUSTOM verschieben.");
  GuideText("FAVORITEN ist eine zusätzliche persönliche Sammlung: Über BLOCK SORTIEREN kannst du einen Block zu FAVORITEN hinzufügen oder wieder daraus entfernen. Der Block bleibt gleichzeitig in seiner Hauptkategorie erhalten. Favoriten werden mit einem Stern markiert.");
  GuideText("Deine persönlichen Kategorien und Favoriten werden gespeichert und stehen nach einem Neustart wieder zur Verfügung. Die eigentlichen Block-IDs und die Welt werden durch das Umsortieren nicht verändert.");
 }
  static void DrawGuideWorkshop(){
  GuideHeading("CUSTOM BLOCK WORKSHOP");
  GuideText("Mit dem Workshop baust du aus einer 8×8×8 großen Arbeitsbox einen eigenen Block. Wähle zuerst den Block „Custom Workshop 8x8“ und setze die Box an einer freien Stelle. Baue anschließend innerhalb der markierten Box dein Modell. Alle dort gesetzten Mini-Voxel werden beim Backen zu einem einzelnen 1×1×1 Custom Block zusammengefasst.");
  GuideText("Im Workshop wählst du einen der Custom-Slots 01–32. Mit TAB wechselst du in den UI-Modus. Die Kamera bleibt dabei stehen und der Mauszeiger wird freigegeben. Nun kannst du Bewegungsanimation, Geschwindigkeit, Partikeleffekt und Partikelstärke einstellen. Die Live-Vorschau neben der Box zeigt das Ergebnis bereits vor dem Backen. Mit einem weiteren TAB kehrst du in den Baumodus zurück.");
  GuideText("Wenn Modell und Effekte passen, wähle „JETZT BACKEN“. Danach findest du den fertigen Custom Block im BlockSet und kannst ihn wie einen normalen Block setzen. Animation und Partikeleffekt gehören zum jeweiligen Custom-Slot. Wird ein Slot später neu gebacken, erhält dieser Slot das neue Modell und die neuen Einstellungen.");
  GuideText("Tipp: Für ruhige Bauteile eignet sich „None“. Pflanzen lassen sich zum Beispiel mit „Wind“ und „Glowing Dust“ kombinieren, technische Objekte mit „Rotate“ oder „Piston“ und Funken, und dekorative Objekte mit „Hover“, „Magic“ oder „Sparkles“.");
 }
  static void DrawGuideCopyPaste(){
  GuideHeading("COPY & PASTE");
  GuideText("Copy & Paste kopiert einen rechteckigen Bereich der Welt. Wähle zuerst „Copy Start“ und klicke direkt auf den Block an der ersten Ecke des gewünschten Bereichs. Wähle danach „Copy End“ und klicke auf den Block an der gegenüberliegenden Ecke. Die Auswahl wird markiert und in den Kopierpuffer übernommen.");
  GuideText("Wähle anschließend „Paste Selection“. Die transparente Vorschau zeigt, wo die Kopie eingesetzt wird. Mit R drehst du die Kopie vor dem Einsetzen jeweils um 90° (0° → 90° → 180° → 270° → 0°). Alternativ: Pfeil rechts/links = ±90°, Pfeil hoch = 180°, Pfeil runter = 0°. Setze sie danach an der gewünschten Position. Kopiert werden die vollständigen Blockdaten innerhalb des markierten Bereichs, einschließlich leerer Stellen. Dadurch bleiben auch Hohlräume und Zwischenräume eines Bauwerks erhalten.");
  GuideText("Eine Auswahl darf maximal 32×32×32 Blöcke groß sein. Enthält die Auswahl überhaupt keinen gesetzten Block, wird sie nicht übernommen. Der Kopierpuffer gilt für die aktuelle Spielsitzung.");
 }
  static void DrawGuideTNT(){
  GuideHeading("CREATIVE TNT");
  GuideText("Creative TNT ist ein Werkzeug zum schnellen Bearbeiten großer Bereiche. Wähle „Creative TNT“ im BlockSet und setze es an der Stelle, die das Zentrum der Entfernung bilden soll. Danach öffnet sich die Radiusauswahl.");
  GuideText("Der Radius kann zwischen 8 und 32 Blöcken eingestellt werden. Mit „ZÜNDEN“ beginnt ein Countdown von drei Sekunden. Anschließend werden die Blöcke innerhalb eines kugelförmigen Bereichs entfernt. Die Änderung wird wie andere Weltbearbeitungen gespeichert.");
  GuideText("Creative TNT verursacht keinen Spielerschaden und ist keine Waffenmechanik. Es ist für Terraforming, das Entfernen großer Bauabschnitte und schnelle Umbauten gedacht. Vor großen Änderungen empfiehlt es sich, die Welt zu speichern.");
 }
  static void DrawGuideHistory(){
  GuideHeading("DIE GESCHICHTE VON BLOCK ISLAND");
  GuideText("Block Island entstand zwischen 2014 und 2019 als privates Voxel-Spielprojekt. Zu Beginn gab es weder besondere Programmierkenntnisse noch einen fertigen Entwicklungsplan. Viele Funktionen entstanden durch Ausprobieren, Umbauen und wiederholtes Testen. So entwickelten sich nach und nach eigene Welten, Gebäude und Spielmechaniken.");
  GuideText("2026 wurde das alte Projekt wieder aufgegriffen. Mit Unterstützung moderner KI-Werkzeuge konnten alte Daten analysiert, frühere Welten wiederhergestellt und technische Probleme der ursprünglichen Version schrittweise gelöst werden.");
  GuideText("Aus der Wiederbelebung von Block Island entstand die heutige Fassung von Block Island. Sie verbindet die restaurierte ursprüngliche Welt mit der weiterentwickelten Voxel-Engine, neuen Creative-Werkzeugen und dem Custom Block Workshop. Die historische Block-Island-Welt bleibt dabei erhalten und kann weiterhin verändert und gespeichert werden.");
 }
  static void DrawGuideMinecart(){
  GuideHeading("MINECART");
  GuideText("Lege zuerst eine zusammenhängende Strecke aus den Minecart-Schienen an und platziere anschließend ein Minecart auf der Strecke. Die Schienen unterstützen gerade Abschnitte, Kurven sowie Höhenwechsel der Strecke.");
  GuideText("Gehe in die Nähe des Minecarts und drücke M, um einzusteigen. Während der Fahrt folgt der Spieler dem Minecart; die Kamera kann weiter benutzt werden. Drücke erneut M, um auszusteigen. Der Spieler wird neben dem Minecart abgesetzt.");
 }
  static void DrawGuideSaveLoad(){
  GuideHeading("SPEICHERN & LADEN");
  GuideText("Im Pause-Menü unter Welt kann der aktuelle Stand manuell gespeichert werden. Block Island besitzt zusätzlich eine eigene Speicherfunktion. Während des Spiels wird außerdem regelmäßig automatisch gespeichert.");
  GuideText("Die Savegames liegen im Ordner Dokumente/Block Island/Saves. Über „Savegame-Ordner öffnen“ kann dieser Ordner direkt aus dem Spiel geöffnet werden. Im Hauptmenü lädt „Welt laden“ einen vorhandenen Spielstand.");
  GuideText("Bei Block Island bleibt die feste Ausgangswelt unverändert. Gespeichert werden die Änderungen, die während des Spielens vorgenommen wurden. Dadurch kann jederzeit weitergebaut werden, ohne die historische Basiswelt neu schreiben zu müssen.");
 }
  static void DrawGuideWeather(){
  GuideHeading("WETTER");
  GuideText("Das Wetter stellst du im Pause-Menü unter WETTER ein. „Wettereffekte anzeigen“ schaltet Regen, Schnee, Gewitter, Wolken und Regenbogen ein oder aus. Mit „Automatisches Zufallswetter“ wechselt das Wetter von selbst alle paar Minuten. Alternativ kannst du Sonne, Bewölkt, Regen, Gewitter, Schnee oder Schneesturm fest einstellen.");
  GuideText("Regen und Schnee fallen nicht durch Dächer. Schnee bleibt drei bis fünf Minuten auf Boden, Dächern und Rasen liegen und schmilzt dann. Der Schneesturm lässt eine Winterlandschaft entstehen; mit dem Regler darunter legst du fest, wie viele Flocken liegen bleiben dürfen (40.000 bis 1.000.000 – hohe Werte brauchen einen schnellen Rechner). Nach dem Regen erscheint manchmal ein Regenbogen am Horizont.");
  GuideText("Die Wettereinstellungen werden gespeichert. Sie verändern nur die Darstellung und Atmosphäre der Welt.");
 }
  static void DrawGuideAudio(){
  GuideHeading("AUDIO-PLAYER");
  GuideText("Der Musikplayer befindet sich im Pause-Menü unter Audio. Über „MP3-Ordner wählen“ wird ein eigener Musikordner ausgewählt. Die gefundenen Titel werden als Playlist geladen. Ist kein Ordner gewählt, wird Dokumente/Block Island/Musik verwendet.");
  GuideText("Mit Zurück, Play, Pause, Stop und Weiter wird die Wiedergabe gesteuert. Shuffle schaltet die zufällige Titelreihenfolge ein oder aus. Die Lautstärke kann unabhängig über den Regler eingestellt werden. Der aktuell abgespielte Titel wird im Audio-Bereich angezeigt.");
 }
  static void DrawGuideTips(){
  GuideHeading("KURZE HILFEN & TIPPS");
  GuideText("E öffnet die Block-Auswahl. Mit dem Mausrad wechselst du durch die Auswahl. Linksklick setzt einen Block bzw. benutzt das ausgewählte Werkzeug, Rechtsklick entfernt einen Block. H speichert einen PNG-Screenshot. M steigt bei einem Minecart ein oder aus. T bringt dich zum Startpunkt zurück, J stellt die Höhe einer Figur ein und P öffnet den Prefab-Importer. ENTER benutzt einen Funktionsblock, N zeigt deine Wegpunkte.");
  GuideText("Im Custom Workshop schaltet TAB zwischen Baumodus und UI-Bedienung um. ESC öffnet weiterhin das Pause-Menü. Für größere Umbauten können Copy & Paste und Creative TNT kombiniert werden. Speichere wichtige Zwischenstände, bevor du große Bereiche veränderst.");
 }

}
