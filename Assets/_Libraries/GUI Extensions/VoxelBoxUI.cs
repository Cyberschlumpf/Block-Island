using UnityEngine;

// The Voxel Box 0.9.3 - full responsive wood / voxel UI skin.
public static class VoxelBoxUI {
    public const float RefW=1600f, RefH=900f;
    static GUIStyle title,subtitle,panel,button,danger,label,small,header,selected,hotbar,tab,tabActive,card,section;
    static Texture2D panelTex,buttonTex,buttonHoverTex,buttonActiveTex,dangerTex,darkTex,cardTex,tabTex,tabActiveTex,slotTex,bgTex,menuArt;
    static bool ready; static Matrix4x4 oldMatrix;

    static Texture2D Solid(Color c){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);for(int y=0;y<2;y++)for(int x=0;x<2;x++)t.SetPixel(x,y,c);t.Apply();t.hideFlags=HideFlags.HideAndDontSave;return t;}
    static float Noise(int x,int y){return Mathf.PerlinNoise(x*.073f,y*.121f);}
    static Texture2D Framed(int w,int h,Color woodA,Color woodB,Color inner,Color rim,bool blue=false){
        var t=new Texture2D(w,h,TextureFormat.RGBA32,false); int b=Mathf.Max(7,Mathf.Min(w,h)/7);
        for(int y=0;y<h;y++)for(int x=0;x<w;x++){
            bool edge=x<b||x>=w-b||y<b||y>=h-b; Color c;
            if(edge){float grain=.25f+.75f*Noise(x,y);c=Color.Lerp(woodA,woodB,grain); if(x<3||y<3)c*=1.25f;if(x>=w-3||y>=h-3)c*=.55f;}
            else {float n=(Noise(x*2,y*2)-.5f)*.06f;c=inner+new Color(n,n,n,0);}
            bool ir=(x==b||x==b+1||x==w-b-1||x==w-b-2||y==b||y==b+1||y==h-b-1||y==h-b-2); if(ir)c=blue?new Color(.05f,.48f,.93f,1):rim;
            int cx=(x<b?b/2:(x>=w-b?w-b/2:-99)), cy=(y<b?b/2:(y>=h-b?h-b/2:-99)); if(cx>=0&&cy>=0&&(x-cx)*(x-cx)+(y-cy)*(y-cy)<5)c=new Color(.62f,.64f,.61f,1);
            t.SetPixel(x,y,c);
        }t.Apply();t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.hideFlags=HideFlags.HideAndDontSave;return t;
    }
    static Texture2D ButtonTex(int w,int h,Color a,Color b,Color rim){
        var t=new Texture2D(w,h,TextureFormat.RGBA32,false);int bd=5;
        for(int y=0;y<h;y++)for(int x=0;x<w;x++){float v=(float)y/(h-1);Color c=Color.Lerp(a,b,v);c*=.90f+.13f*Noise(x,y);if(x<bd||x>=w-bd||y<bd||y>=h-bd)c=rim;if(y<2)c*=1.4f;if(y>=h-2)c*=.55f;t.SetPixel(x,y,c);}t.Apply();t.hideFlags=HideFlags.HideAndDontSave;return t;
    }
    static void Disc(Texture2D t,int cx,int cy,int r,Color c){int w=t.width,h=t.height;for(int y=Mathf.Max(0,cy-r);y<Mathf.Min(h,cy+r);y++)for(int x=Mathf.Max(0,cx-r);x<Mathf.Min(w,cx+r);x++){int dx=x-cx,dy=y-cy;if(dx*dx+dy*dy<=r*r)t.SetPixel(x,y,c);}}
    static void RectFill(Texture2D t,int x0,int y0,int x1,int y1,Color c){for(int y=Mathf.Max(0,y0);y<Mathf.Min(t.height,y1);y++)for(int x=Mathf.Max(0,x0);x<Mathf.Min(t.width,x1);x++)t.SetPixel(x,y,c);}
    static Texture2D MakeBackground(){
        int w=800,h=450;var t=new Texture2D(w,h,TextureFormat.RGB24,false);
        for(int y=0;y<h;y++)for(int x=0;x<w;x++){float v=(float)y/h;Color sky=Color.Lerp(new Color(1f,.46f,.28f),new Color(.12f,.48f,.88f),Mathf.Clamp01((v-.12f)/.88f));t.SetPixel(x,y,sky);}
        Disc(t,410,225,34,new Color(1f,.88f,.36f)); Disc(t,410,225,18,new Color(1f,.96f,.68f));
        // distant mountains
        for(int x=0;x<w;x++){float m1=165+72*Mathf.PerlinNoise(x*.009f,2.3f)+55*Mathf.Sin(x*.017f);float m2=115+55*Mathf.PerlinNoise(x*.015f,7.1f)+35*Mathf.Sin(x*.031f+1);for(int y=0;y<(int)m1;y++)t.SetPixel(x,y,new Color(.12f,.24f,.23f));for(int y=0;y<(int)m2;y++)t.SetPixel(x,y,new Color(.16f,.31f,.24f));}
        // water and warm reflection
        RectFill(t,0,0,w,92,new Color(.07f,.30f,.47f));for(int y=12;y<92;y+=7)for(int x=0;x<w;x+=18){if(Noise(x,y)>.43f)RectFill(t,x,y,x+12,y+2,new Color(.20f,.53f,.65f));}
        for(int y=20;y<115;y++)for(int x=370;x<455;x++){float q=1-Mathf.Abs(x-412)/43f;if(q>Noise(x,y)*.8f)t.SetPixel(x,y,Color.Lerp(t.GetPixel(x,y),new Color(1f,.58f,.22f),q*.45f));}
        // voxel trees foreground
        for(int i=0;i<22;i++){int x=(i*73+31)%w;int y=92+(i%4)*10;int th=28+(i%5)*7;RectFill(t,x,y,x+8,y+th,new Color(.20f,.11f,.05f));for(int k=0;k<3;k++)RectFill(t,x-13+k*4,y+th-4+k*7,x+22-k*4,y+th+10+k*7,new Color(.10f+.02f*k,.34f+.03f*k,.12f));}
        // cabin silhouette + lit windows
        RectFill(t,560,103,680,170,new Color(.22f,.12f,.06f));for(int y=170;y<205;y++){int half=(205-y)*2;RectFill(t,620-half,y,620+half,y+1,new Color(.16f,.08f,.035f));}RectFill(t,580,125,598,145,new Color(1f,.57f,.12f));RectFill(t,642,125,660,145,new Color(1f,.57f,.12f));
        // rail bridge
        RectFill(t,475,78,790,87,new Color(.18f,.15f,.14f));for(int x=480;x<790;x+=18)RectFill(t,x,72,x+5,92,new Color(.31f,.20f,.12f));RectFill(t,475,74,790,77,new Color(.58f,.45f,.31f));RectFill(t,475,88,790,91,new Color(.58f,.45f,.31f));
        t.Apply();t.filterMode=FilterMode.Bilinear;t.hideFlags=HideFlags.HideAndDontSave;return t;
    }
    public static void Ensure(){if(ready)return;ready=true;
        panelTex=Framed(96,96,new Color(.20f,.09f,.035f),new Color(.50f,.28f,.10f),new Color(.035f,.045f,.05f,.98f),new Color(.72f,.48f,.20f));
        cardTex=Framed(72,72,new Color(.15f,.07f,.03f),new Color(.36f,.19f,.07f),new Color(.045f,.06f,.065f,.98f),new Color(.38f,.27f,.15f));
        buttonTex=ButtonTex(128,48,new Color(.31f,.17f,.07f),new Color(.17f,.075f,.025f),new Color(.58f,.36f,.15f));
        buttonHoverTex=ButtonTex(128,48,new Color(.48f,.28f,.10f),new Color(.25f,.12f,.04f),new Color(.90f,.60f,.20f));
        buttonActiveTex=ButtonTex(128,48,new Color(.08f,.55f,.96f),new Color(.02f,.25f,.64f),new Color(.38f,.80f,1f));
        dangerTex=ButtonTex(128,48,new Color(.72f,.18f,.10f),new Color(.42f,.06f,.035f),new Color(.95f,.40f,.20f));
        tabTex=ButtonTex(96,42,new Color(.16f,.19f,.20f),new Color(.07f,.09f,.10f),new Color(.40f,.31f,.20f));
        tabActiveTex=ButtonTex(96,42,new Color(.05f,.48f,.94f),new Color(.02f,.20f,.56f),new Color(.35f,.80f,1f));
        slotTex=Solid(new Color(.02f,.025f,.03f,.88f));darkTex=Solid(new Color(.01f,.015f,.02f,.78f));bgTex=MakeBackground();menuArt=Resources.Load<Texture2D>("VoxelBoxUI/MainMenuBackground");Font f=GUI.skin.font;
        title=new GUIStyle(GUI.skin.label){font=f,fontSize=50,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(1f,.80f,.34f)}};
        subtitle=new GUIStyle(GUI.skin.label){fontSize=17,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(.90f,.91f,.85f)}};
        label=new GUIStyle(GUI.skin.label){fontSize=15,normal={textColor=new Color(.96f,.96f,.91f)},wordWrap=true};small=new GUIStyle(label){fontSize=12,normal={textColor=new Color(.74f,.80f,.76f)}};
        header=new GUIStyle(label){fontSize=22,fontStyle=FontStyle.Bold,normal={textColor=new Color(1f,.79f,.31f)}};
        section=new GUIStyle(header){fontSize=17,normal={textColor=new Color(.93f,.93f,.88f)}};
        panel=new GUIStyle(GUI.skin.box){normal={background=panelTex,textColor=Color.white},padding=new RectOffset(25,25,24,24),border=new RectOffset(14,14,14,14)};
        card=new GUIStyle(GUI.skin.box){normal={background=cardTex},padding=new RectOffset(18,18,16,16),margin=new RectOffset(0,0,5,5),border=new RectOffset(11,11,11,11)};
        button=new GUIStyle(GUI.skin.button){fontSize=17,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(20,12,8,8),normal={background=buttonTex,textColor=Color.white},hover={background=buttonHoverTex,textColor=Color.white},active={background=buttonActiveTex,textColor=Color.white},focused={background=buttonHoverTex,textColor=Color.white},border=new RectOffset(7,7,7,7)};
        danger=new GUIStyle(button);danger.normal.background=dangerTex;danger.hover.background=dangerTex;
        selected=new GUIStyle(GUI.skin.box){normal={background=buttonActiveTex},padding=new RectOffset(5,5,5,5),border=new RectOffset(7,7,7,7)};
        hotbar=new GUIStyle(GUI.skin.box){normal={background=cardTex},padding=new RectOffset(10,10,8,8),border=new RectOffset(10,10,10,10)};
        tab=new GUIStyle(button){alignment=TextAnchor.MiddleCenter,fontSize=14,normal={background=tabTex,textColor=Color.white}};tabActive=new GUIStyle(tab);tabActive.normal.background=tabActiveTex;
    }
    public static void BeginResponsive(){Ensure();oldMatrix=GUI.matrix;float s=Mathf.Min(Screen.width/RefW,Screen.height/RefH);float ox=(Screen.width-RefW*s)*.5f,oy=(Screen.height-RefH*s)*.5f;GUI.matrix=Matrix4x4.TRS(new Vector3(ox,oy,0),Quaternion.identity,new Vector3(s,s,1));}
    public static void EndResponsive(){GUI.matrix=oldMatrix;}
    public static Rect Center(float w,float h){return new Rect((RefW-w)*.5f,(RefH-h)*.5f,w,h);}public static Rect Center(float wf,float hf,bool fraction){return Center(RefW*wf,RefH*hf);}public static Rect Safe(float w,float h,float top=28){return new Rect((RefW-w)*.5f,top,w,Mathf.Min(h,RefH-top*2));}
    public static GUIStyle Title{get{Ensure();return title;}}public static GUIStyle Subtitle{get{Ensure();return subtitle;}}public static GUIStyle Panel{get{Ensure();return panel;}}public static GUIStyle Button{get{Ensure();return button;}}public static GUIStyle Danger{get{Ensure();return danger;}}public static GUIStyle Label{get{Ensure();return label;}}public static GUIStyle Small{get{Ensure();return small;}}public static GUIStyle Header{get{Ensure();return header;}}public static GUIStyle Section{get{Ensure();return section;}}public static GUIStyle Selected{get{Ensure();return selected;}}public static GUIStyle Hotbar{get{Ensure();return hotbar;}}public static GUIStyle Tab{get{Ensure();return tab;}}public static GUIStyle TabActive{get{Ensure();return tabActive;}}public static GUIStyle Card{get{Ensure();return card;}}
    public static void Backdrop(float a=.35f){Ensure();Color o=GUI.color;GUI.color=new Color(1,1,1,a);GUI.DrawTexture(new Rect(-2000,-2000,6000,5000),darkTex);GUI.color=o;}
    public static void MenuBackground(){
        Ensure(); Texture2D t=menuArt!=null?menuArt:bgTex;
        // 1.09: draw the complete menu artwork over the complete native screen.
        // StretchToFill deliberately shows the whole image on 16:9, 21:9 and 32:9 instead of cropping it.
        Matrix4x4 scaled=GUI.matrix; GUI.matrix=Matrix4x4.identity;
        GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),t,ScaleMode.StretchToFill);
        GUI.matrix=scaled;
    }
    public static bool BigButton(string t){return GUILayout.Button(t,Button,GUILayout.Height(56),GUILayout.ExpandWidth(true));}
    public static bool MenuButton(string icon,string text,string sub){
        // A real IMGUI button owns the click. The old hand-written MouseUp hit test
        // could lose clicks after responsive GUI.matrix scaling.
        GUIStyle s=new GUIStyle(Button){fontSize=18,wordWrap=true,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(22,12,7,6)};
        if(string.IsNullOrEmpty(sub)) return GUILayout.Button(icon+"   "+text,s,GUILayout.Height(52),GUILayout.ExpandWidth(true));
        return GUILayout.Button(icon+"   "+text+"\n      "+sub,s,GUILayout.Height(66),GUILayout.ExpandWidth(true));
    }
    public static void Logo(){GUILayout.Label("THE",new GUIStyle(Title){fontSize=24});GUILayout.Label("VOXEL BOX",new GUIStyle(Title){fontSize=46});GUILayout.Label("TANVIIR  •  CREATIVE WORLD",Subtitle);}
    public static void SectionTitle(string text){GUILayout.Label(text,Section);GUILayout.Space(5);}
    public static void PanelTitle(string icon,string text,string sub=null){GUILayout.BeginHorizontal();GUILayout.Label(icon,Header,GUILayout.Width(34));GUILayout.Label(text,Header);GUILayout.EndHorizontal();if(!string.IsNullOrEmpty(sub))GUILayout.Label(sub,Small);GUILayout.Space(8);}
}
