using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Sapphire
{
    /// <summary>Application-only uGUI presentation. No item, quest or combat rule is decided here.</summary>
    public sealed class GamePresenter : MonoBehaviour
    {
        public Texture2D[] Art;
        public Font UiFont;
        public Camera CaptureCamera { get; private set; }
        GameApp app; GameInput input; Canvas canvas; RectTransform safe;
        GameObject title, hud, controls, townActions, modal, death, result;
        Text hpText,mpText,levelText,goldText,zoneText,objectiveText,noticeText,autoText,bossText,resultText,deathText;
        Image hpFill,mpFill,xpFill,bossFill; GameObject boss;
        Text hpPotion,mpPotion; Button continueButton;
        readonly Text[] skillTexts=new Text[4]; readonly Image[] skillFills=new Image[4]; readonly Button[] skillButtons=new Button[4];
        Text dodgeText; string page="",menuSignature=""; int inventoryPage; string lastMode;
        readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>(StringComparer.OrdinalIgnoreCase);
        readonly Color ink=new Color(.035f,.055f,.085f,.96f), panel=new Color(.055f,.085f,.13f,.94f), line=new Color(.29f,.40f,.50f,.7f), blue=new Color(.20f,.55f,.83f), muted=new Color(.61f,.72f,.81f), gold=new Color(.90f,.76f,.48f);
        Sprite circle;
        void Start()
        {
            app=GetComponent<GameApp>();if(app==null)app=GameApp.Instance;
            if(!UiFont) UiFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var cam=new GameObject("Game camera");CaptureCamera=cam.AddComponent<Camera>();CaptureCamera.orthographic=true;CaptureCamera.orthographicSize=5.625f;CaptureCamera.transform.position=new Vector3(0,0,-20);CaptureCamera.backgroundColor=ink;CaptureCamera.clearFlags=CameraClearFlags.SolidColor;
            var cv=new GameObject("Sapphire interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas=cv.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=CaptureCamera;canvas.planeDistance=10;canvas.sortingOrder=2000;
            var scaler=cv.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=.5f;
            safe=Rect("Safe area",cv.transform,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.offsetMin=safe.offsetMax=Vector2.zero;
            if(FindFirstObjectByType<EventSystem>()==null)new GameObject("Event system",typeof(EventSystem),typeof(StandaloneInputModule));
            circle=CircleSprite();
            input=gameObject.AddComponent<GameInput>();input.Presenter=this;
            BuildTitle();BuildHud();BuildControls();BuildTown();BuildOutcomes();
            var world=new GameObject("World presentation").AddComponent<WorldRenderer>();world.Initialize(app,this);
        }
        public Sprite ArtSprite(params string[] names)
        {
            if(Art==null)return null;
            foreach(string name in names){Sprite existing;if(sprites.TryGetValue(name,out existing))return existing;foreach(var texture in Art)if(texture && string.Equals(texture.name,name,StringComparison.OrdinalIgnoreCase)){var s=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);sprites[name]=s;return s;}}
            return null;
        }
        public Sprite AtlasSprite(string name,int col,int row,int columns=4,int rows=4)
        {
            string key=name+"/"+col+"/"+row;Sprite s;if(sprites.TryGetValue(key,out s))return s;
            if(Art!=null)foreach(var t in Art)if(t && t.name==name){int w=t.width/columns,h=t.height/rows;s=Sprite.Create(t,new Rect(col*w,(rows-1-row)*h,w,h),new Vector2(.5f,.5f),100);sprites[key]=s;return s;}
            return null;
        }
        Sprite CircleSprite(){var t=new Texture2D(64,64,TextureFormat.RGBA32,false);var p=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++)p[y*64+x]=new Color(1,1,1,Mathf.Clamp01(31.5f-Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))));t.SetPixels(p);t.Apply();return Sprite.Create(t,new Rect(0,0,64,64),new Vector2(.5f,.5f),64);}
        RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size){var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;return r;}
        Image Box(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color color,bool round=false){var r=Rect(name,parent,anchor,pos,size);var image=r.gameObject.AddComponent<Image>();image.color=color;if(round)image.sprite=circle;return image;}
        Text Label(string value,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,int fontSize,Color color,TextAnchor align=TextAnchor.MiddleLeft)
        {var r=Rect("Text",parent,anchor,pos,size);var t=r.gameObject.AddComponent<Text>();t.font=UiFont;t.text=value;t.fontSize=fontSize;t.color=color;t.alignment=align;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;return t;}
        Button Button(string text,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Action action,bool primary=false,bool round=false)
        {var image=Box(text,parent,anchor,pos,size,primary?new Color(.12f,.36f,.55f,.96f):panel,round);var outline=image.gameObject.AddComponent<Outline>();outline.effectColor=primary?blue:line;outline.effectDistance=new Vector2(1,-1);var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1.25f,1.25f,1.25f);c.pressedColor=new Color(.7f,.85f,1);b.colors=c;b.onClick.AddListener(()=>action());Label(text,image.transform,new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(8,4),18,Color.white,TextAnchor.MiddleCenter);return b;}
        Image Bar(string name,Transform parent,Vector2 pos,Vector2 size,Color color)
        {var bg=Box(name,parent,new Vector2(0,1),pos,size,new Color(.015f,.03f,.05f,.95f));var fill=Box("Fill",bg.transform,new Vector2(0,.5f),Vector2.zero,size,color);var r=fill.rectTransform;r.pivot=new Vector2(0,.5f);return fill;}
        void BuildTitle()
        {
            title=Rect("Title",safe,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero).gameObject;
            var shade=Box("Title shade",title.transform,new Vector2(.5f,.5f),new Vector2(-340,0),new Vector2(710,1000),new Color(.015f,.03f,.06f,.9f));
            Label("S A P P H I R E",title.transform,new Vector2(.5f,.5f),new Vector2(-295,190),new Vector2(490,50),40,gold);
            Label("달빛의 균열",title.transform,new Vector2(.5f,.5f),new Vector2(-295,125),new Vector2(490,60),49,Color.white);
            Box("Accent",title.transform,new Vector2(.5f,.5f),new Vector2(-474,69),new Vector2(130,3),blue);
            Label("고요한 숲에 열린 균열.\n사파이어의 빛으로 첫 여정을 시작하세요.",title.transform,new Vector2(.5f,.5f),new Vector2(-295,13),new Vector2(490,66),20,muted);
            continueButton=Button("여정 이어가기",title.transform,new Vector2(.5f,.5f),new Vector2(-350,-93),new Vector2(380,58),()=>app.ContinueGame(),true);
            Button("새로운 여정",title.transform,new Vector2(.5f,.5f),new Vector2(-350,-164),new Vector2(380,58),()=>{if(app.CanContinue)ShowNewGameConfirm();else app.NewGame();});
            Button("설정",title.transform,new Vector2(.5f,.5f),new Vector2(-446,-237),new Vector2(188,48),()=>ShowMenu("설정"));
            Button("종료",title.transform,new Vector2(.5f,.5f),new Vector2(-248,-237),new Vector2(176,48),()=>Application.Quit());
            Label("CHAPTER 01   ·   달빛을 잃은 숲",title.transform,new Vector2(.5f,.5f),new Vector2(338,-295),new Vector2(430,36),16,new Color(.8f,.87f,.93f),TextAnchor.MiddleRight);
        }
        void BuildHud()
        {
            hud=Rect("HUD",safe,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero).gameObject;var h=(RectTransform)hud.transform;h.anchorMin=Vector2.zero;h.anchorMax=Vector2.one;h.offsetMin=h.offsetMax=Vector2.zero;
            Box("Status backing",h,new Vector2(0,1),new Vector2(186,-64),new Vector2(336,98),panel);
            levelText=Label("",h,new Vector2(0,1),new Vector2(60,-58),new Vector2(64,65),20,gold,TextAnchor.MiddleCenter);
            hpFill=Bar("HP",h,new Vector2(226,-44),new Vector2(227,19),new Color(.72f,.24f,.35f));mpFill=Bar("MP",h,new Vector2(226,-72),new Vector2(227,15),new Color(.22f,.48f,.86f));xpFill=Bar("XP",h,new Vector2(226,-91),new Vector2(227,3),gold);
            hpText=Label("",h,new Vector2(0,1),new Vector2(226,-44),new Vector2(210,22),13,Color.white,TextAnchor.MiddleCenter);mpText=Label("",h,new Vector2(0,1),new Vector2(226,-71),new Vector2(210,20),12,Color.white,TextAnchor.MiddleCenter);
            zoneText=Label("",h,new Vector2(.5f,1),new Vector2(0,-28),new Vector2(460,36),22,Color.white,TextAnchor.MiddleCenter);
            goldText=Label("",h,new Vector2(1,1),new Vector2(-185,-35),new Vector2(150,36),20,gold,TextAnchor.MiddleRight);
            Button("메뉴  ≡",h,new Vector2(1,1),new Vector2(-66,-37),new Vector2(104,48),()=>ToggleMenu());
            objectiveText=Label("",h,new Vector2(0,1),new Vector2(214,-138),new Vector2(392,55),16,new Color(.92f,.89f,.74f));
            noticeText=Label("",h,new Vector2(.5f,0),new Vector2(0,32),new Vector2(610,42),16,Color.white,TextAnchor.MiddleCenter);
            boss=Rect("Boss health",h,new Vector2(.5f,1),new Vector2(0,-80),new Vector2(420,40)).gameObject;
            bossFill=Bar("Guardian HP",boss.transform,new Vector2(210,-30),new Vector2(390,7),new Color(.8f,.27f,.4f));bossText=Label("",boss.transform,new Vector2(.5f,.5f),new Vector2(0,7),new Vector2(410,30),16,gold,TextAnchor.MiddleCenter);
        }
        void BuildControls()
        {
            controls=Rect("Combat controls",hud.transform,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero).gameObject;var r=(RectTransform)controls.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            var stick=Box("Movement",r,new Vector2(0,0),new Vector2(107,119),new Vector2(142,142),new Color(.05f,.1f,.16f,.65f),true);var outline=stick.gameObject.AddComponent<Outline>();outline.effectColor=line;
            input.Stick=stick.gameObject.AddComponent<VirtualStick>();input.Stick.Knob=Box("Stick knob",stick.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(64,64),new Color(.34f,.53f,.68f,.65f),true).rectTransform;
            Label("W A S D",r,new Vector2(0,0),new Vector2(107,30),new Vector2(160,20),12,muted,TextAnchor.MiddleCenter);
            var atk=Button("공격\nJ",r,new Vector2(1,0),new Vector2(-107,123),new Vector2(104,104),()=>app.Attack(),true,true);input.AttackHold=atk.gameObject.AddComponent<HoldCommand>();
            string[] names={"비전탄","서리 파동","점멸","보호막"};Vector2[] p={new Vector2(-213,76),new Vector2(-235,170),new Vector2(-170,252),new Vector2(-71,243)};
            for(int i=0;i<4;i++){int slot=i;var b=Button("",r,new Vector2(1,0),p[i],new Vector2(77,77),()=>app.Cast(slot),false,true);skillButtons[i]=b;skillFills[i]=Box("Cooldown",b.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(75,75),new Color(.015f,.025f,.06f,.85f),true);skillFills[i].type=Image.Type.Filled;skillFills[i].fillMethod=Image.FillMethod.Radial360;skillFills[i].fillOrigin=2;skillFills[i].raycastTarget=false;skillTexts[i]=Label(names[i]+"\n"+(i+1),b.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(75,67),14,Color.white,TextAnchor.MiddleCenter);}
            var dodge=Button("",r,new Vector2(1,0),new Vector2(-323,70),new Vector2(80,64),()=>app.Dodge());dodgeText=dodge.GetComponentInChildren<Text>();
            var hp=Button("",r,new Vector2(1,0),new Vector2(-429,70),new Vector2(74,64),()=>app.UsePotion(true));hpPotion=hp.GetComponentInChildren<Text>();hpPotion.color=new Color(1,.55f,.60f);
            var mp=Button("",r,new Vector2(1,0),new Vector2(-511,70),new Vector2(74,64),()=>app.UsePotion(false));mpPotion=mp.GetComponentInChildren<Text>();mpPotion.color=new Color(.5f,.76f,1);
            var auto=Button("",r,new Vector2(1,0),new Vector2(-324,148),new Vector2(80,48),()=>app.ToggleAuto());autoText=auto.GetComponentInChildren<Text>();autoText.fontSize=14;
            Button("귀환",r,new Vector2(1,1),new Vector2(-65,-98),new Vector2(100,46),()=>ShowReturnConfirm());
        }
        void BuildTown()
        {
            townActions=Rect("Town services",hud.transform,new Vector2(.5f,0),new Vector2(0,106),new Vector2(640,100)).gameObject;
            Label("사파이어 광장   ·   여정을 준비하세요",townActions.transform,new Vector2(.5f,.5f),new Vector2(0,57),new Vector2(600,30),18,Color.white,TextAnchor.MiddleCenter);
            Button("안내자 · 의뢰",townActions.transform,new Vector2(.5f,.5f),new Vector2(-255,0),new Vector2(156,62),()=>ShowMenu("의뢰"));
            Button("상인 · 물약",townActions.transform,new Vector2(.5f,.5f),new Vector2(-85,0),new Vector2(156,62),()=>ShowMenu("상점"));
            Button("대장장이 · 장비",townActions.transform,new Vector2(.5f,.5f),new Vector2(85,0),new Vector2(156,62),()=>ShowMenu("장비"));
            Button("출발",townActions.transform,new Vector2(.5f,.5f),new Vector2(255,0),new Vector2(156,62),()=>ShowMenu("출발"),true);
        }
        void BuildOutcomes()
        {
            death=Box("Defeat",safe,new Vector2(.5f,.5f),Vector2.zero,new Vector2(640,290),ink).gameObject;
            Label("빛이 잠시 사그라졌습니다",death.transform,new Vector2(.5f,.5f),new Vector2(0,77),new Vector2(570,54),28,gold,TextAnchor.MiddleCenter);
            deathText=Label("",death.transform,new Vector2(.5f,.5f),new Vector2(0,6),new Vector2(560,65),18,muted,TextAnchor.MiddleCenter);
            Button("체크포인트에서 부활",death.transform,new Vector2(.5f,.5f),new Vector2(0,-88),new Vector2(340,55),()=>app.Respawn(),true);
            result=Box("Dungeon result",safe,new Vector2(.5f,.5f),Vector2.zero,new Vector2(680,350),ink).gameObject;
            Label("균열의 기록",result.transform,new Vector2(.5f,.5f),new Vector2(0,112),new Vector2(600,60),34,gold,TextAnchor.MiddleCenter);
            resultText=Label("",result.transform,new Vector2(.5f,.5f),new Vector2(0,16),new Vector2(594,110),19,Color.white,TextAnchor.MiddleCenter);
            Button("마을로 돌아가기",result.transform,new Vector2(.5f,.5f),new Vector2(0,-113),new Vector2(360,58),()=>app.ReturnTown(),true);
        }
        void Update()
        {
            if(app==null || safe==null)return;
            var area=Screen.safeArea;safe.anchorMin=new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);safe.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);
            CaptureCamera.orthographicSize=Mathf.Max(5.625f,10f/CaptureCamera.aspect);
            bool isTitle=app.Mode=="Title";title.SetActive(isTitle);hud.SetActive(!isTitle);continueButton.interactable=app.CanContinue;
            controls.SetActive(app.Mode=="Field"||app.Mode=="Dungeon");townActions.SetActive(false);death.SetActive(app.Dead && app.Mode!="Result");result.SetActive(app.Mode=="Result");
            if(lastMode!=app.Mode){lastMode=app.Mode;CloseMenu();}
            levelText.text="Lv.\n"+app.Level;hpText.text="HP   "+app.Hp+" / "+app.MaxHp;mpText.text="MP   "+app.Mp+" / "+app.MaxMp;
            hpFill.rectTransform.sizeDelta=new Vector2(227*Mathf.Clamp01((float)app.Hp/Mathf.Max(1,app.MaxHp)),19);mpFill.rectTransform.sizeDelta=new Vector2(227*Mathf.Clamp01((float)app.Mp/Mathf.Max(1,app.MaxMp)),15);xpFill.rectTransform.sizeDelta=new Vector2(227*Mathf.Clamp01((float)app.Xp/Mathf.Max(1,app.NextXp)),3);
            goldText.text=app.Gold.ToString("N0")+" G";zoneText.text=app.ZoneName;objectiveText.text=app.Objective;noticeText.text=app.Notice;
            hpPotion.text="HP ×"+app.HpPotions+"\nQ";mpPotion.text="MP ×"+app.MpPotions+"\nF";autoText.text="AUTO "+(app.Auto?"ON":"OFF")+"\nT";autoText.color=app.Auto?gold:muted;dodgeText.text=app.DodgeRemaining>0?app.DodgeRemaining.ToString("0.0"):"회피\nSHIFT";
            string[] names={"비전탄","서리 파동","점멸","보호막"};for(int i=0;i<4;i++){float cd=app.Cooldowns!=null && i<app.Cooldowns.Length?app.Cooldowns[i]:0;float max=app.MaxCooldowns!=null && i<app.MaxCooldowns.Length?app.MaxCooldowns[i]:1;bool unlocked=app.SkillUnlocked!=null && i<app.SkillUnlocked.Length && app.SkillUnlocked[i];skillButtons[i].interactable=unlocked && cd<=0;skillFills[i].fillAmount=unlocked?Mathf.Clamp01(cd/Mathf.Max(.1f,max)):1;skillTexts[i].text=unlocked?names[i]+"\n"+(cd>0?cd.ToString("0.0"):(i+1).ToString()):"잠김\nLv."+(i+1);}
            bool hasBoss=false;if(app.Actors!=null)foreach(var a in app.Actors)if(a.Boss&&!a.Dead){hasBoss=true;bossText.text=a.Name+"   "+a.Hp+" / "+a.MaxHp;bossFill.rectTransform.sizeDelta=new Vector2(390*Mathf.Clamp01((float)a.Hp/Mathf.Max(1,a.MaxHp)),7);break;}boss.SetActive(hasBoss);
            deathText.text="이미 획득한 필드 보상은 유지됩니다.\nHP와 MP를 회복해 다시 출발하세요.";
            resultText.text=app.Objective+"\n"+app.Notice+"\n"+app.SaveStatus;
            if(modal && page!="확인"){string signature=app.Gold+"/"+app.HpPotions+"/"+app.MpPotions+"/"+app.QuestIndex+"/"+inventoryPage;foreach(var item in app.Inventory)signature+=item.Id+item.Upgrade+item.Equipped;foreach(var q in app.Quests)signature+=q.Status+q.Progress;if(signature!=menuSignature){menuSignature=signature;RebuildMenu();}}
        }
        public void ToggleMenu(){if(modal)CloseMenu();else ShowMenu(app.Mode=="Title"?"설정":"가방");}
        public void PauseOnFocusLoss(){if(app!=null && app.Mode!="Title" && app.Mode!="Result")ShowMenu("설정");}
        public void ShowMenu(string name){page=name;inventoryPage=0;menuSignature="";app.SetPaused(true);if(input)input.ResetInput();RebuildMenu();}
        public void CloseMenu(){if(modal)Destroy(modal);modal=null;page="";if(app!=null)app.SetPaused(false);if(input)input.ResetInput();}
        void ModalFrame(string heading)
        {
            if(modal)Destroy(modal);modal=Box("Modal",safe,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1080,574),ink).gameObject;
            var outline=modal.AddComponent<Outline>();outline.effectColor=line;
            Label(heading,modal.transform,new Vector2(.5f,.5f),new Vector2(-190,238),new Vector2(620,48),28,gold);
            Label(app.SaveStatus,modal.transform,new Vector2(.5f,.5f),new Vector2(267,235),new Vector2(260,36),13,muted,TextAnchor.MiddleRight);
            Button("닫기 ×",modal.transform,new Vector2(.5f,.5f),new Vector2(462,236),new Vector2(116,48),()=>CloseMenu());
            Box("Divider",modal.transform,new Vector2(.5f,.5f),new Vector2(0,195),new Vector2(1010,1),line);
        }
        void RebuildMenu()
        {
            if(page=="")return;ModalFrame(page);
            if(page=="설정"){Settings();return;}if(page=="출발"){TravelMenu();return;}if(page=="상점"){Shop();return;}
            string[] tabs={"가방","장비","의뢰","설정"};for(int i=0;i<tabs.Length;i++){string tab=tabs[i];Button(tab,modal.transform,new Vector2(.5f,.5f),new Vector2(-390+i*260,163),new Vector2(248,48),()=>ShowMenu(tab),page==tab);}
            if(page=="의뢰"){QuestMenu();return;}InventoryMenu();
        }
        void InventoryMenu()
        {
            var list=app.Inventory;int first=inventoryPage*4;Label("보유 골드   "+app.Gold+" G     ·     장비 "+list.Count+" / 24",modal.transform,new Vector2(.5f,.5f),new Vector2(0,109),new Vector2(1000,36),17,muted);
            for(int n=0;n<4 && first+n<list.Count;n++){var item=list[first+n];string id=item.Id;float y=49-n*74;Box("Item row",modal.transform,new Vector2(.5f,.5f),new Vector2(0,y),new Vector2(1000,67),panel);Label(item.Name+" +"+item.Upgrade+(item.Equipped?"  [장착 중]":""),modal.transform,new Vector2(.5f,.5f),new Vector2(-251,y+12),new Vector2(460,28),18,item.Equipped?gold:Color.white);Label(item.Description,modal.transform,new Vector2(.5f,.5f),new Vector2(-251,y-14),new Vector2(460,25),13,muted);Button("장착",modal.transform,new Vector2(.5f,.5f),new Vector2(51,y),new Vector2(96,48),()=>app.Equip(id),!item.Equipped);Button("강화",modal.transform,new Vector2(.5f,.5f),new Vector2(165,y),new Vector2(110,48),()=>app.Upgrade(id));Button("판매 "+item.Price+" G",modal.transform,new Vector2(.5f,.5f),new Vector2(337,y),new Vector2(200,48),()=>app.Sell(id));}
            Label("강화 비용  +1 30 G / +2 60 G / +3 100 G   ·   장착 중인 장비는 판매할 수 없습니다",modal.transform,new Vector2(.5f,.5f),new Vector2(-80,-229),new Vector2(850,30),13,muted);
            if(first>0)Button("이전",modal.transform,new Vector2(.5f,.5f),new Vector2(349,-239),new Vector2(90,43),()=>{inventoryPage--;menuSignature="";});if(first+4<list.Count)Button("다음",modal.transform,new Vector2(.5f,.5f),new Vector2(453,-239),new Vector2(90,43),()=>{inventoryPage++;menuSignature="";});
        }
        void QuestMenu()
        {
            int row=0;foreach(var q in app.Quests){float y=99-row*45;Label((row+1)+"  "+q.Name,modal.transform,new Vector2(.5f,.5f),new Vector2(-290,y),new Vector2(415,37),16,Color.white);Label(q.Status+"   "+q.Progress+" / "+q.Target,modal.transform,new Vector2(.5f,.5f),new Vector2(44,y),new Vector2(215,37),15,gold);row++;if(row==6)break;}
            if(app.QuestIndex>=0 && app.QuestIndex<app.Quests.Count)Label(app.Quests[app.QuestIndex].Description,modal.transform,new Vector2(.5f,.5f),new Vector2(310,19),new Vector2(320,185),18,muted);
            Button("의뢰 수락",modal.transform,new Vector2(.5f,.5f),new Vector2(202,-217),new Vector2(174,52),()=>app.AcceptQuest(),true);Button("보상 받기",modal.transform,new Vector2(.5f,.5f),new Vector2(398,-217),new Vector2(174,52),()=>app.TurnInQuest(),true);
        }
        void Shop()
        {
            Label("상인 루미   ·   필요한 만큼 준비하세요",modal.transform,new Vector2(.5f,.5f),new Vector2(-120,128),new Vector2(760,45),23,Color.white);
            Label("보유 골드  "+app.Gold+" G",modal.transform,new Vector2(.5f,.5f),new Vector2(0,69),new Vector2(1000,45),20,gold);
            Label("체력 물약    HP 60 회복    보유 "+app.HpPotions,modal.transform,new Vector2(.5f,.5f),new Vector2(-132,-8),new Vector2(735,54),21,Color.white);Button("HP 물약 구매",modal.transform,new Vector2(.5f,.5f),new Vector2(355,-8),new Vector2(230,56),()=>app.BuyPotion(true),true);
            Label("마나 물약    MP 45 회복    보유 "+app.MpPotions,modal.transform,new Vector2(.5f,.5f),new Vector2(-132,-91),new Vector2(735,54),21,Color.white);Button("MP 물약 구매",modal.transform,new Vector2(.5f,.5f),new Vector2(355,-91),new Vector2(230,56),()=>app.BuyPotion(false),true);
            Label("거래 결과: "+app.Notice,modal.transform,new Vector2(.5f,.5f),new Vector2(0,-198),new Vector2(1000,58),17,muted);
        }
        void TravelMenu()
        {
            Label("목적지를 선택하세요",modal.transform,new Vector2(.5f,.5f),new Vector2(0,142),new Vector2(900,42),23,Color.white);
            if(app.DestinationNames!=null)for(int i=0;i<app.DestinationNames.Length;i++){int dest=i;Button(app.DestinationNames[i],modal.transform,new Vector2(.5f,.5f),new Vector2(0,60-i*74),new Vector2(700,60),()=>{CloseMenu();app.Travel(dest);},true);}
            Label("던전: 성공 시 전리품이 확정됩니다. 실패·중도 귀환 시 임시 전리품은 사라지고 사용한 물약은 복구되지 않습니다.",modal.transform,new Vector2(.5f,.5f),new Vector2(0,-225),new Vector2(950,56),16,muted);
        }
        void Settings()
        {
            Label("음량",modal.transform,new Vector2(.5f,.5f),new Vector2(-190,132),new Vector2(620,40),22,Color.white);
            Label("음악",modal.transform,new Vector2(.5f,.5f),new Vector2(-430,70),new Vector2(140,45),18,muted);VolumeControl(true,70);Label("효과음",modal.transform,new Vector2(.5f,.5f),new Vector2(-430,5),new Vector2(140,45),18,muted);VolumeControl(false,5);
            Label("이동 WASD / 방향키   ·   공격 J   ·   스킬 1–4   ·   회피 Shift\nHP 물약 Q   ·   MP 물약 F   ·   상호작용 E   ·   AUTO T   ·   메뉴 Esc\n\n메뉴를 열거나 창을 벗어나면 전투가 일시정지됩니다.",modal.transform,new Vector2(.5f,.5f),new Vector2(0,-116),new Vector2(1000,137),18,muted);
            Button("지금 저장",modal.transform,new Vector2(.5f,.5f),new Vector2(365,-229),new Vector2(258,48),()=>{app.SaveNow();RebuildMenu();},true);
        }
        void VolumeControl(bool music,float y)
        {float value=music?app.MusicVolume:app.SfxVolume;Label(Mathf.RoundToInt(value*100)+"%",modal.transform,new Vector2(.5f,.5f),new Vector2(0,y),new Vector2(270,40),20,Color.white,TextAnchor.MiddleCenter);Button("−",modal.transform,new Vector2(.5f,.5f),new Vector2(-190,y),new Vector2(62,48),()=>{app.SetVolumes(music?Mathf.Clamp01(app.MusicVolume-.1f):app.MusicVolume,music?app.SfxVolume:Mathf.Clamp01(app.SfxVolume-.1f));RebuildMenu();});Button("+",modal.transform,new Vector2(.5f,.5f),new Vector2(190,y),new Vector2(62,48),()=>{app.SetVolumes(music?Mathf.Clamp01(app.MusicVolume+.1f):app.MusicVolume,music?app.SfxVolume:Mathf.Clamp01(app.SfxVolume+.1f));RebuildMenu();});}
        void ShowNewGameConfirm(){page="확인";ModalFrame("새로운 여정");Label("현재 저장된 여정을 새 프로필로 교체합니다.",modal.transform,new Vector2(.5f,.5f),new Vector2(0,45),new Vector2(900,100),24,Color.white,TextAnchor.MiddleCenter);Button("새 여정 시작",modal.transform,new Vector2(.5f,.5f),new Vector2(0,-77),new Vector2(360,60),()=>{CloseMenu();app.NewGame();},true);}
        void ShowReturnConfirm(){ShowMenu("확인");ModalFrame("마을로 귀환");Label(app.Mode=="Dungeon"?"던전의 임시 전리품은 사라집니다.\n사용한 물약은 복구되지 않습니다.":"이미 획득한 보상을 가지고 마을로 돌아갑니다.",modal.transform,new Vector2(.5f,.5f),new Vector2(0,40),new Vector2(900,115),23,Color.white,TextAnchor.MiddleCenter);Button("귀환하기",modal.transform,new Vector2(.5f,.5f),new Vector2(0,-92),new Vector2(360,60),()=>{CloseMenu();app.ReturnTown();},true);}
        public void Capture(string absolutePath)
        {
            if(!CaptureCamera)throw new InvalidOperationException("Presenter not initialized");
            string directory=Path.GetDirectoryName(absolutePath);if(!string.IsNullOrEmpty(directory))Directory.CreateDirectory(directory);
            var target=new RenderTexture(1280,720,24);var old=CaptureCamera.targetTexture;var active=RenderTexture.active;
            CaptureCamera.targetTexture=target;CaptureCamera.Render();RenderTexture.active=target;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(absolutePath,texture.EncodeToPNG());CaptureCamera.targetTexture=old;RenderTexture.active=active;Destroy(texture);target.Release();Destroy(target);
        }
    }
}
