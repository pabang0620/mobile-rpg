using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lighthaven2D
{
    public sealed class BattleScreen : MonoBehaviour
    {
        public Texture2D background, mageIdle, magePortrait, goblin, skillSheet, menuIconSheet, menuPanelTexture, hudControlSheet, uiChromeSheet;
        public Font uiFont;
        public Texture2D[] mageAttackPoses;
        // New asset bundle: parallax background layers, skill VFX, contrast goblin variants, UI icon/border placeholders.
        public Texture2D parallaxFarLayer, parallaxNearLayer, vfxMagicMissileSheet, goblinVariantPixelArt, goblinVariantMonster, inventoryIconSheet, fantasyPanelBorder;
        readonly List<Image> enemies = new List<Image>();
        readonly List<Image> enemyBars = new List<Image>();
        readonly List<Text> enemyTexts = new List<Text>();
        readonly List<Text> damageTexts = new List<Text>();
        readonly Image[] skills = new Image[4];
        readonly Image[] menuSlots = new Image[8];
        readonly Text[] cooldowns = new Text[4];
        BattleSession battle;
        Canvas canvas; Camera sceneCamera; Transform worldLayer; GameObject menuRoot; Image hero, hpFill, mpFill, xpFill, targetHpFill, autoLamp, autoRune, shield, telegraph, menuHeader; Outline autoOutline;
        Text levelText, statusText, hpText, mpText, targetText, autoText, retryText, menuTitle, menuBody, killPopup, hpPotionCount, mpPotionCount;
        Sprite idleSprite, portraitSprite, goblinSprite, menuPanelSprite, circleSprite; Sprite[] attackSprites; Sprite[] skillSprites; Sprite[] menuSprites; Sprite[] hudSprites; Sprite[] chromeSprites;
        Sprite goblinVariantPixelArtSprite, goblinVariantMonsterSprite; Sprite[] vfxMagicMissileSprites; Sprite[] inventorySprites; Sprite panelBorderSprite;
        Font font; int impactCursor, presentedKills; bool smoke; Coroutine killPopupRoutine;

        void Awake()
        {
            Application.targetFrameRate = 60;
            font = uiFont ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sceneCamera = Camera.main;
            if (!sceneCamera) { var go = new GameObject("Camera", typeof(Camera)); sceneCamera = go.GetComponent<Camera>(); go.tag = "MainCamera"; }
            sceneCamera.clearFlags = CameraClearFlags.SolidColor; sceneCamera.backgroundColor = Color.black; sceneCamera.orthographic = true;
            idleSprite = Trimmed(mageIdle); portraitSprite = Full(magePortrait); goblinSprite = Trimmed(goblin); circleSprite = CircleSprite(128);
            attackSprites = new Sprite[mageAttackPoses.Length]; for (int i = 0; i < attackSprites.Length; i++) attackSprites[i] = Trimmed(mageAttackPoses[i]);
            skillSprites = Slice(skillSheet, 4, 2);
            menuSprites = Slice(menuIconSheet, 4, 2); hudSprites = Slice(hudControlSheet, 4, 2); chromeSprites=SliceTrimmed(uiChromeSheet,4,2); menuPanelSprite = Full(menuPanelTexture);
            if(goblinVariantPixelArt) goblinVariantPixelArtSprite = TrimmedRect(goblinVariantPixelArt, new Rect(9*32,0,32,32));
            if(goblinVariantMonster) goblinVariantMonsterSprite = Trimmed(goblinVariantMonster);
            if(vfxMagicMissileSheet) vfxMagicMissileSprites = Slice(vfxMagicMissileSheet, 42, 1);
            if(inventoryIconSheet) inventorySprites = Slice(inventoryIconSheet, 4, 2);
            if(fantasyPanelBorder) panelBorderSprite = SlicedBorder(fantasyPanelBorder, 16);
            BuildUi(); ResetRun();
            smoke = Array.Exists(Environment.GetCommandLineArgs(), a => a == "--smoke");
            if (smoke) StartCoroutine(Smoke());
        }

        void BuildUi()
        {
            var root = new GameObject("Battle UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = sceneCamera; canvas.planeDistance = 2;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            if (!UnityEngine.Object.FindAnyObjectByType<EventSystem>()) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Image("Background", root.transform, Full(background), new Vector2(640,360), new Vector2(1280,720), Color.white, false);
            if(parallaxFarLayer) Image("Parallax Far", root.transform, Full(parallaxFarLayer), new Vector2(640,610), new Vector2(1280,150), new Color(1,1,1,.85f), false);
            if(parallaxNearLayer) Image("Parallax Near", root.transform, Full(parallaxNearLayer), new Vector2(640,120), new Vector2(1280,210), Color.white, false);
            var worldObject = new GameObject("World Layer", typeof(RectTransform)); worldObject.transform.SetParent(root.transform,false); worldLayer=worldObject.transform;
            var worldRect=worldObject.GetComponent<RectTransform>();worldRect.anchorMin=Vector2.zero;worldRect.anchorMax=Vector2.one;worldRect.offsetMin=worldRect.offsetMax=Vector2.zero;
            hero = Image("Mage", worldLayer, idleSprite, new Vector2(350,BattleSession.GroundTop+BattleSession.HeroHalfHeight), new Vector2(180,180), Color.white, false); hero.preserveAspect = true;
            shield = Image("Shield", worldLayer, circleSprite, Vector2.zero, new Vector2(145,145), new Color(.08f,.55f,1,.22f), false); Circle(shield, new Color(.25f,.75f,1,.38f));
            telegraph = Image("Enemy Telegraph", worldLayer, null, Vector2.zero, new Vector2(160,55), new Color(1,.22f,.08f,.22f), false); Outline(telegraph, new Color(1,.36f,.08f,.85f));

            var statusPanel=Panel(root.transform,"Compact Status",new Vector2(152,672),new Vector2(284,72),new Color(.01f,.02f,.03f,.56f),new Color(.68f,.74f,.78f,.22f));
            var portraitFrame=Image("Portrait Frame",statusPanel.transform,chromeSprites[0],new Vector2(-108,0),new Vector2(58,58),new Color(1,1,1,.76f),false);
            var portrait=Image("Mage Portrait",portraitFrame.transform,portraitSprite,new Vector2(0,-2),new Vector2(52,52),Color.white,false);portrait.preserveAspect=true;
            levelText = Text("Level", statusPanel.transform, "LV. 1", new Vector2(-61,23), new Vector2(74,20), 15, Color.white, TextAnchor.MiddleLeft);
            hpFill = Bar(statusPanel.transform, new Vector2(48,7), new Vector2(166,12), new Color(.9f,.05f,.16f), out hpText);
            mpFill = Bar(statusPanel.transform, new Vector2(48,-15), new Vector2(166,10), new Color(.05f,.35f,.95f), out mpText);

            Text("Target Name",root.transform,"고블린 정예",new Vector2(640,566),new Vector2(300,28),18,new Color(.94f,.96f,1),TextAnchor.MiddleCenter);
            targetHpFill=Bar(root.transform,new Vector2(640,540),new Vector2(500,22),new Color(.86f,.06f,.15f),out targetText);

            Text("Stage Name",root.transform,"◆  달빛 회랑  ◆",new Vector2(640,688),new Vector2(300,26),18,new Color(.82f,.94f,1),TextAnchor.MiddleCenter);
            statusText = Text("Status", root.transform, "", new Vector2(640,50), new Vector2(360,24), 14, new Color(.90f,.88f,.80f,.86f), TextAnchor.MiddleCenter);
            killPopup = Text("Kill Progress",root.transform,"",new Vector2(640,395),new Vector2(230,58),34,new Color(.75f,.9f,1,0),TextAnchor.MiddleCenter);

            autoLamp = Image("AutoLamp", root.transform, chromeSprites[0], new Vector2(1210,330), new Vector2(62,62), new Color(1,1,1,.72f), true); Outline(autoLamp, Color.clear);autoOutline=autoLamp.GetComponent<Outline>();
            autoRune=Image("Auto Rune",autoLamp.transform,hudSprites[5],Vector2.zero,new Vector2(58,58),new Color(.65f,.72f,.82f,1),false);autoRune.preserveAspect=true;
            autoText = Text("AutoText", autoLamp.transform, "AUTO", new Vector2(0,-45), new Vector2(70,18), 12, Color.white, TextAnchor.MiddleCenter);
            autoLamp.GetComponent<Button>().onClick.AddListener(() => battle.ToggleAuto());

            var joystick=Image("Move Pad",root.transform,chromeSprites[0],new Vector2(108,112),new Vector2(170,170),new Color(1,1,1,.22f),false);
            var knob=Image("Move Knob",joystick.transform,circleSprite,Vector2.zero,new Vector2(64,64),new Color(.72f,.78f,.84f,.28f),false);Text("Move Rune",knob.transform,"●",Vector2.zero,new Vector2(60,60),20,new Color(.78f,.84f,.9f,.72f),TextAnchor.MiddleCenter);

            Vector2[] skillPositions={new Vector2(1088,142),new Vector2(1098,205),new Vector2(1152,244),new Vector2(1214,226)};
            for (int i = 0; i < 4; i++)
            {
                int slot = i; var frame=RoundControl("Skill "+(i+1),root.transform,skillSprites[Mathf.Min(i,skillSprites.Length-1)],skillPositions[i],66,true,out var icon);skills[i]=icon;
                frame.GetComponent<Button>().onClick.AddListener(() => battle.Attack(slot));
                cooldowns[i] = Text("Cooldown", frame.transform, "", Vector2.zero, new Vector2(62,62), 20, Color.white, TextAnchor.MiddleCenter);
            }
            var attack=RoundControl("Attack",root.transform,hudSprites[4],new Vector2(1190,96),116,true,out var attackIcon);attack.GetComponent<Button>().onClick.AddListener(()=>battle.Attack());
            var jump=RoundControl("Jump",root.transform,hudSprites[2],new Vector2(1082,63),62,true,out var jumpIcon);jump.GetComponent<Button>().onClick.AddListener(()=>battle.Jump());
            var dodge=RoundControl("Dodge",root.transform,hudSprites[3],new Vector2(1014,63),58,true,out var dodgeIcon);dodge.GetComponent<Button>().onClick.AddListener(()=>battle.Dodge(battle.Facing));
            var hpPotion=RoundControl("HP Potion",root.transform,hudSprites[0],new Vector2(1080,318),48,true,out var hpPotionIcon);hpPotionCount=Text("HP Potion Count",hpPotion.transform,"10",new Vector2(18,-18),new Vector2(28,18),12,Color.white,TextAnchor.MiddleCenter);hpPotion.GetComponent<Button>().onClick.AddListener(()=>battle.UseHpPotion());
            var mpPotion=RoundControl("MP Potion",root.transform,hudSprites[1],new Vector2(1136,318),48,true,out var mpPotionIcon);mpPotionCount=Text("MP Potion Count",mpPotion.transform,"10",new Vector2(18,-18),new Vector2(28,18),12,Color.white,TextAnchor.MiddleCenter);mpPotion.GetComponent<Button>().onClick.AddListener(()=>battle.UseMpPotion());
            var retryPanel = Panel(root.transform, "Retry Panel", new Vector2(640,360), new Vector2(330,70), new Color(.05f,.06f,.11f,.92f), new Color(.72f,.56f,.28f,1));
            retryText = Text("Retry", retryPanel.transform, "다시 도전 [R]", Vector2.zero, new Vector2(320,64), 30, Color.white, TextAnchor.MiddleCenter); retryPanel.transform.SetAsLastSibling(); retryPanel.gameObject.SetActive(false);
            Text("XP Label",root.transform,"EXP",new Vector2(320,16),new Vector2(48,18),12,new Color(.75f,.82f,.86f),TextAnchor.MiddleRight);
            var xpBg = Image("XP", root.transform, chromeSprites[6], new Vector2(640,16), new Vector2(590,10), new Color(1,1,1,.48f), false);
            xpFill = Image("XP Fill", xpBg.transform, null, new Vector2(-293,0), new Vector2(586,8), new Color(.18f,.74f,1,1), false); xpFill.rectTransform.pivot = new Vector2(0,.5f);
            BuildMenus(root.transform);
        }

        void BuildMenus(Transform root)
        {
            var hamburger=SquareControl("Main Menu",root,hudSprites[6],new Vector2(1235,678),58,true,out var hamburgerIcon);hamburger.GetComponent<Button>().onClick.AddListener(()=>OpenMenu(0));

            var dim=Image("Menu Modal",root,null,new Vector2(640,360),new Vector2(1280,720),new Color(0,0,.015f,.22f),true);menuRoot=dim.gameObject;dim.GetComponent<Button>().onClick.AddListener(CloseMenu);
            var panel=Panel(dim.transform,"Right Menu Panel",new Vector2(400,0),new Vector2(480,720),new Color(.018f,.026f,.035f,.82f),new Color(.62f,.68f,.72f,.34f));panel.raycastTarget=true;
            if(panelBorderSprite){var border=Image("Right Menu Panel Border",panel.transform,panelBorderSprite,Vector2.zero,new Vector2(480,720),new Color(1,1,1,.55f),false);border.type=UnityEngine.UI.Image.Type.Sliced;}
            menuHeader=Image("Menu Header",panel.transform,hudSprites[7],new Vector2(-184,315),new Vector2(46,46),Color.white,false);menuHeader.preserveAspect=true;
            menuTitle=Text("Menu Title",panel.transform,"메뉴",new Vector2(-105,315),new Vector2(140,42),25,Color.white,TextAnchor.MiddleLeft);
            menuBody=Text("Menu Body",panel.transform,"",new Vector2(0,244),new Vector2(400,58),15,new Color(.86f,.88f,.90f),TextAnchor.UpperLeft);
            string[] labels={"가방","스킬","장비","퀘스트","던전","레이드","상점","설정"};
            for(int i=0;i<8;i++){var x=-153+(i%4)*102;var y=150-(i/4)*118;var initialIcon=InventoryPlaceholder(i)??menuSprites[i%menuSprites.Length];SquareControl("Menu Slot Frame "+i,panel.transform,initialIcon,new Vector2(x,y),72,false,out menuSlots[i]);Text("Menu Label "+i,panel.transform,labels[i],new Vector2(x,y-52),new Vector2(90,24),15,new Color(.94f,.95f,.96f),TextAnchor.MiddleCenter);}
            Text("Menu Section",panel.transform,"모험과 성장",new Vector2(-148,-78),new Vector2(160,24),15,new Color(.68f,.74f,.78f),TextAnchor.MiddleLeft);
            var divider=Image("Menu Divider",panel.transform,null,new Vector2(0,-98),new Vector2(408,1),new Color(.72f,.76f,.8f,.24f),false);
            var close=SquareControl("Close Menu",panel.transform,hudSprites[6],new Vector2(202,315),42,true,out var closeIcon);closeIcon.rectTransform.localRotation=Quaternion.Euler(0,0,90);close.GetComponent<Button>().onClick.AddListener(CloseMenu);
            menuRoot.SetActive(false);
        }

        void OpenMenu(int index)
        {
            string[] titles={"메인 메뉴","사파이어 마법","던전 선택","레이드 준비"};
            string[] bodies={
                "전투 중 획득한 장비와 재료를 확인합니다.\n현재 세션 골드와 보상은 전투 종료 전 임시 상태입니다.",
                "비전 화살 · 수정 파동 · 점멸 · 마나 보호막\n같은 전투 규칙을 수동 조작과 자동사냥이 함께 사용합니다.",
                "달빛 회랑  ·  권장 레벨 1\n일반 전투 → 정예 전투 → 보스 → 결과 정산 흐름으로 확장됩니다.",
                "봉인된 청룡  ·  준비 중\n자동사냥으로 성장한 뒤 패턴 대응과 장비 구성을 시험하는 목표 콘텐츠입니다."};
            menuHeader.sprite=menuSprites[index];menuTitle.text=titles[index];menuBody.text=index==0?"가방 · 스킬 · 장비 · 퀘스트\n던전 · 레이드 · 상점 · 설정":bodies[index];
            for(int i=0;i<menuSlots.Length;i++){menuSlots[i].gameObject.SetActive(true);menuSlots[i].sprite=InventoryPlaceholder(i)??(index==1?skillSprites[i%skillSprites.Length]:menuSprites[i%menuSprites.Length]);}
            menuRoot.SetActive(true);menuRoot.transform.SetAsLastSibling();
        }

        void CloseMenu(){if(menuRoot)menuRoot.SetActive(false);}
        Sprite InventoryPlaceholder(int slotIndex){if(inventorySprites==null||inventorySprites.Length<8)return null;if(slotIndex==0)return inventorySprites[0];if(slotIndex==6)return inventorySprites[2];return null;}

        void ResetRun()
        {
            battle?.Close(); battle = new BattleSession(); impactCursor = 0; presentedKills = 0;
            foreach(var e in enemies) Destroy(e.gameObject); enemies.Clear(); enemyBars.Clear(); enemyTexts.Clear();
            for(int i=0;i<battle.Enemies.Count;i++)
            {
                // Slot 0 stays the canonical Goblin.png. Slots 1/2 show the two candidate contrast
                // variants (GOBLIN FREE PIXELART, then OpenGameArt "Goblin monster") side by side with it
                // for direct tone comparison, per asset-review instructions.
                var variantSprite=goblinSprite;if(i==1&&goblinVariantPixelArtSprite!=null)variantSprite=goblinVariantPixelArtSprite;else if(i==2&&goblinVariantMonsterSprite!=null)variantSprite=goblinVariantMonsterSprite;
                var img=Image("Goblin"+i,worldLayer,variantSprite,battle.Enemies[i].Position,new Vector2(140,140),Color.white,false);img.preserveAspect=true;enemies.Add(img);
                var bar=Image("EnemyHP",worldLayer,null,battle.Enemies[i].Position+new Vector2(0,80),new Vector2(92,7),new Color(.7f,.08f,.1f,1),false);enemyBars.Add(bar);
                enemyTexts.Add(Text("EnemyName",worldLayer,"고블린",battle.Enemies[i].Position+new Vector2(0,96),new Vector2(100,18),12,Color.white,TextAnchor.MiddleCenter));
            }
            if(hero){hero.sprite=idleSprite;retryText.gameObject.SetActive(false);}
        }

        void Update()
        {
            if (battle == null) return;
            if(menuRoot&&menuRoot.activeSelf){if(Input.GetKeyDown(KeyCode.Escape))CloseMenu();Present();return;}
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) battle.Attack();
            if (Input.GetKeyDown(KeyCode.Space)) battle.Jump();
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) battle.Dodge(input);
            for(int i=0;i<4;i++) if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i))) battle.Attack(i);
            if (Input.GetKeyDown(KeyCode.T)) battle.ToggleAuto();
            if (Input.GetKeyDown(KeyCode.Alpha5)) OpenMenu(0);if (Input.GetKeyDown(KeyCode.Alpha6)) OpenMenu(1);if (Input.GetKeyDown(KeyCode.Alpha7)) OpenMenu(2);if (Input.GetKeyDown(KeyCode.Alpha8)) OpenMenu(3);
            if (Input.GetKeyDown(KeyCode.R) && battle.Dead) ResetRun();
            battle.Tick(Time.deltaTime,input); Present();
        }

        void Present()
        {
            hero.rectTransform.anchoredPosition=battle.Hero;
            if(battle.Attacking && attackSprites.Length>0) hero.sprite=attackSprites[Mathf.Clamp(Mathf.FloorToInt(battle.AttackAge/.94f*attackSprites.Length),0,attackSprites.Length-1)]; else hero.sprite=idleSprite;
            hero.color=battle.Dead?new Color(.35f,.35f,.45f,.65f):Color.white;
            shield.gameObject.SetActive(battle.Time < battle.ShieldUntil); shield.rectTransform.anchoredPosition=battle.Hero;
            var warning=battle.Enemies.Find(e=>e.Windup>0&&!e.Dead);telegraph.gameObject.SetActive(warning!=null);if(warning!=null)telegraph.rectTransform.anchoredPosition=warning.Position+(battle.Hero-warning.Position).normalized*55;
            for(int i=0;i<battle.Enemies.Count;i++)
            {
                var e=battle.Enemies[i];enemies[i].rectTransform.anchoredPosition=e.Position;enemies[i].gameObject.SetActive(!e.Dead);enemies[i].color=e.Flash>0?new Color(1,.35f,.35f):Color.white;
                enemyBars[i].rectTransform.anchoredPosition=e.Position+new Vector2(0,80);enemyBars[i].rectTransform.sizeDelta=new Vector2(92f*e.Hp/e.MaxHp,7);enemyBars[i].gameObject.SetActive(!e.Dead);
                enemyTexts[i].rectTransform.anchoredPosition=e.Position+new Vector2(0,96);enemyTexts[i].gameObject.SetActive(!e.Dead);
            }
            while(impactCursor<battle.Impacts.Count){var impact=battle.Impacts[impactCursor++];var t=Text("Damage",worldLayer,"-"+impact.Amount,impact.Position+new Vector2(0,125),new Vector2(90,28),22,impact.Hero?new Color(1,.35f,.3f):new Color(.35f,.82f,1),TextAnchor.MiddleCenter);damageTexts.Add(t);StartCoroutine(FloatText(t));StartCoroutine(ImpactFlash(impact.Position,impact.Hero));}
            hpFill.rectTransform.localScale=new Vector3((float)battle.Hp/battle.MaxHp,1,1);mpFill.rectTransform.localScale=new Vector3(battle.Mp/100,1,1);xpFill.rectTransform.localScale=new Vector3(Mathf.Clamp01((float)battle.Xp/(battle.Level*100)),1,1);
            var tracked=battle.Nearest();targetHpFill.rectTransform.localScale=new Vector3(tracked==null?0:(float)tracked.Hp/tracked.MaxHp,1,1);targetText.text=tracked==null?"대상 없음":$"{tracked.Hp} / {tracked.MaxHp}";
            hpText.text=$"{battle.Hp}/{battle.MaxHp}";mpText.text=$"{Mathf.FloorToInt(battle.Mp)}/100";levelText.text=$"LV. {battle.Level}";statusText.text=battle.LastAction;
            bool autoOn=battle.Auto;float pulse=.60f+.22f*Mathf.Sin(Time.unscaledTime*4);autoLamp.sprite=chromeSprites[autoOn?1:0];autoLamp.color=new Color(1,1,1,autoOn?.92f:.72f);autoRune.color=autoOn?new Color(.55f,.9f,1,.9f):new Color(.68f,.72f,.76f,.62f);autoOutline.effectColor=autoOn?new Color(.35f,.86f,1,pulse):Color.clear;autoText.text=autoOn?"AUTO ON":"AUTO";if(autoOn)autoRune.rectTransform.Rotate(0,0,-105*Time.unscaledDeltaTime);
            if(battle.Kills>presentedKills){presentedKills=battle.Kills;if(killPopupRoutine!=null)StopCoroutine(killPopupRoutine);killPopupRoutine=StartCoroutine(ShowKillProgress(presentedKills));}
            hpPotionCount.text=battle.HpPotions.ToString();mpPotionCount.text=battle.MpPotions.ToString();
            for(int i=0;i<4;i++){cooldowns[i].text=battle.Cooldowns[i]>.05f?battle.Cooldowns[i].ToString("0.0"):battle.Mp<battle.Costs[i]?"MP":"";skills[i].color=battle.Cooldowns[i]>0||battle.Mp<battle.Costs[i]?new Color(.32f,.36f,.48f):Color.white;}
            retryText.gameObject.SetActive(battle.Dead);
        }

        IEnumerator FloatText(Text t){float age=0;var start=t.rectTransform.anchoredPosition;while(age<.7f){age+=Time.deltaTime;t.rectTransform.anchoredPosition=start+Vector2.up*age*50;t.color=new Color(t.color.r,t.color.g,t.color.b,1-age/.7f);yield return null;}damageTexts.Remove(t);Destroy(t.gameObject);}
        IEnumerator ShowKillProgress(int kills){killPopup.text=$"{(kills % 5 == 0 ? 5 : kills % 5)} / 5";float age=0;while(age<.55f){age+=Time.deltaTime;float a=Mathf.Sin(Mathf.Clamp01(age/.55f)*Mathf.PI);killPopup.color=new Color(.72f,.9f,1,a*.82f);yield return null;}killPopup.color=new Color(.72f,.9f,1,0);killPopupRoutine=null;}
        IEnumerator ImpactFlash(Vector2 position,bool heroHit){var holder=Image("Impact Flash",worldLayer,null,position,new Vector2(80,80),Color.clear,false);var color=heroHit?new Color(1,.25f,.18f,.9f):new Color(.12f,.72f,1,.95f);var rayA=Image("Ray A",holder.transform,null,Vector2.zero,new Vector2(68,8),color,false);var rayB=Image("Ray B",holder.transform,null,Vector2.zero,new Vector2(68,8),color,false);rayA.rectTransform.localRotation=Quaternion.Euler(0,0,45);rayB.rectTransform.localRotation=Quaternion.Euler(0,0,-45);
            Image vfx=null;if(!heroHit&&vfxMagicMissileSprites!=null&&vfxMagicMissileSprites.Length>0)vfx=Image("Skill VFX",worldLayer,vfxMagicMissileSprites[0],position,new Vector2(130,130),Color.white,false);
            float age=0;while(age<.22f){age+=Time.deltaTime;float t=Mathf.Clamp01(age/.22f);holder.rectTransform.localScale=Vector3.one*Mathf.Lerp(.35f,1.45f,t);var faded=new Color(color.r,color.g,color.b,1-t);rayA.color=rayB.color=faded;if(vfx){int frame=Mathf.Clamp(14+Mathf.FloorToInt(t*13),14,vfxMagicMissileSprites.Length-1);vfx.sprite=vfxMagicMissileSprites[frame];vfx.color=new Color(1,1,1,1-t);}yield return null;}Destroy(holder.gameObject);if(vfx)Destroy(vfx.gameObject);}
        IEnumerator Smoke()
        {
            float deadline=Time.realtimeSinceStartup+18;while(battle.Kills<1&&Time.realtimeSinceStartup<deadline)yield return null;
            bool alphaAssets=UsableAlpha(mageIdle)&&UsableAlpha(magePortrait)&&UsableAlpha(goblin)&&Array.TrueForAll(mageAttackPoses,UsableAlpha);
            bool menuAssets=menuSprites.Length==8&&UsableAlpha(menuPanelTexture);
            bool passed=battle.Kills>0&&!battle.Dead&&background&&mageIdle&&goblin&&skillSprites.Length>=4&&alphaAssets&&menuAssets;
            string dir=Arg("--capture-dir")??Path.Combine(Application.dataPath,"../../captures");Directory.CreateDirectory(dir);
            yield return new WaitForEndOfFrame();Capture(Path.Combine(dir,"battle.png"));while(battle.Attacking)yield return null;bool jumped=battle.Jump();yield return new WaitForSeconds(.28f);bool airborne=!battle.Grounded&&battle.Hero.y>BattleSession.GroundTop+BattleSession.HeroHalfHeight;Capture(Path.Combine(dir,"jump.png"));float landingDeadline=Time.realtimeSinceStartup+3;while(!battle.Grounded&&Time.realtimeSinceStartup<landingDeadline)yield return null;passed&=jumped&&airborne&&battle.Grounded;OpenMenu(0);yield return new WaitForEndOfFrame();Capture(Path.Combine(dir,"menu.png"));
            File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(new SmokeReport{passed=passed,kills=battle.Kills,level=battle.Level,gold=battle.Gold,alphaAssets=alphaAssets,menuAssets=menuAssets,jumped=jumped&&airborne},true));
            Debug.Log(passed?"LIGHTHAVEN_2D_RUNTIME PASS":"LIGHTHAVEN_2D_RUNTIME FAIL");Application.Quit(passed?0:1);
        }
        void Capture(string path){Canvas.ForceUpdateCanvases();var rt=new RenderTexture(1280,720,24);sceneCamera.targetTexture=rt;sceneCamera.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());sceneCamera.targetTexture=null;RenderTexture.active=null;Destroy(rt);Destroy(tex);}
        string Arg(string n){var a=Environment.GetCommandLineArgs();for(int i=0;i<a.Length-1;i++)if(a[i]==n)return a[i+1];return null;}
        static bool UsableAlpha(Texture2D texture){if(!texture||!texture.isReadable)return false;bool transparent=false,visible=false;foreach(var pixel in texture.GetPixels32()){transparent|=pixel.a==0;visible|=pixel.a>0;if(transparent&&visible)return true;}return false;}
        [Serializable]class SmokeReport{public bool passed,alphaAssets,menuAssets,jumped;public int kills,level,gold;}

        static Sprite Full(Texture2D t)=>Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.08f),100);
        static Sprite Trimmed(Texture2D texture){if(!texture.isReadable)return Full(texture);var pixels=texture.GetPixels32();int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>8){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}return maxX<minX?Full(texture):Sprite.Create(texture,new Rect(minX,minY,maxX-minX+1,maxY-minY+1),new Vector2(.5f,0),100);}
        static Sprite CircleSprite(int size){var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};var pixels=new Color32[size*size];float c=(size-1)*.5f,r=c-1;for(int y=0;y<size;y++)for(int x=0;x<size;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(c,c));byte a=(byte)Mathf.RoundToInt(Mathf.Clamp01(r-d+1)*255);pixels[y*size+x]=new Color32(255,255,255,a);}texture.SetPixels32(pixels);texture.Apply();return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100);}
        static Sprite[] Slice(Texture2D t,int columns,int rows){var result=new Sprite[columns*rows];int w=t.width/columns,h=t.height/rows;for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)result[(rows-1-y)*columns+x]=Sprite.Create(t,new Rect(x*w,y*h,w,h),new Vector2(.5f,.5f),100);return result;}
        static Sprite[] SliceTrimmed(Texture2D t,int columns,int rows){var result=new Sprite[columns*rows];int w=t.width/columns,h=t.height/rows;var pixels=t.GetPixels32();for(int y=0;y<rows;y++)for(int x=0;x<columns;x++){int x0=x*w,y0=y*h,minX=x0+w,minY=y0+h,maxX=-1,maxY=-1;for(int py=y0;py<y0+h;py++)for(int px=x0;px<x0+w;px++)if(pixels[py*t.width+px].a>8){minX=Mathf.Min(minX,px);minY=Mathf.Min(minY,py);maxX=Mathf.Max(maxX,px);maxY=Mathf.Max(maxY,py);}var rect=maxX<minX?new Rect(x0,y0,w,h):new Rect(minX,minY,maxX-minX+1,maxY-minY+1);result[(rows-1-y)*columns+x]=Sprite.Create(t,rect,new Vector2(.5f,.5f),100);}return result;}
        // Alpha-trims a single cell of a multi-frame strip and anchors the pivot at the visual foot line,
        // matching Trimmed()'s convention so imported enemy variants sit on the same ground line as Goblin.png.
        static Sprite TrimmedRect(Texture2D texture,Rect cell){if(!texture.isReadable)return Sprite.Create(texture,cell,new Vector2(.5f,0),100);var pixels=texture.GetPixels32();int cx0=(int)cell.x,cy0=(int)cell.y,cw=(int)cell.width,ch=(int)cell.height;int minX=cx0+cw,minY=cy0+ch,maxX=cx0-1,maxY=cy0-1;for(int y=cy0;y<cy0+ch;y++)for(int x=cx0;x<cx0+cw;x++)if(pixels[y*texture.width+x].a>8){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}return maxX<minX?Sprite.Create(texture,cell,new Vector2(.5f,0),100):Sprite.Create(texture,new Rect(minX,minY,maxX-minX+1,maxY-minY+1),new Vector2(.5f,0),100);}
        // 9-slice sprite with a uniform border inset (pixels), used for the Fantasy UI Borders overlay frame.
        static Sprite SlicedBorder(Texture2D t,float border)=>Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(border,border,border,border));
        Image RoundControl(string name,Transform parent,Sprite sprite,Vector2 pos,float size,bool button,out Image icon){var frame=Image(name,parent,chromeSprites[0],pos,new Vector2(size,size),new Color(1,1,1,.78f),button);icon=Image(name+" Icon",frame.transform,sprite,Vector2.zero,new Vector2(size*.68f,size*.68f),new Color(1,1,1,.92f),false);icon.preserveAspect=true;return frame;}
        Image SquareControl(string name,Transform parent,Sprite sprite,Vector2 pos,float size,bool button,out Image icon){var frame=Image(name,parent,chromeSprites[2],pos,new Vector2(size,size),new Color(1,1,1,.76f),button);icon=Image(name+" Icon",frame.transform,sprite,Vector2.zero,new Vector2(size*.62f,size*.62f),new Color(.9f,.92f,.94f,.9f),false);icon.preserveAspect=true;return frame;}
        Image Image(string name,Transform parent,Sprite sprite,Vector2 pos,Vector2 size,Color color,bool button){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);var img=go.GetComponent<Image>();img.sprite=sprite;img.color=color;img.raycastTarget=button;var rt=img.rectTransform;rt.anchorMin=rt.anchorMax=parent==canvas.transform||parent==worldLayer?Vector2.zero:new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=pos;rt.sizeDelta=size;if(button)go.AddComponent<Button>().targetGraphic=img;return img;}
        Image Panel(Transform p,string n,Vector2 pos,Vector2 size,Color c,Color line){var i=Image(n,p,null,pos,size,c,false);Outline(i,line);return i;}
        Image Circle(Image i,Color line){Outline(i,line);return i;}
        void Outline(Image i,Color c){var o=i.gameObject.AddComponent<Outline>();o.effectColor=c;o.effectDistance=new Vector2(2,-2);}
        Text Text(string name,Transform parent,string value,Vector2 pos,Vector2 size,int fontSize,Color color,TextAnchor align){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(parent,false);var t=go.GetComponent<Text>();t.font=font;t.text=value;t.fontSize=fontSize;t.color=color;t.alignment=align;t.raycastTarget=false;t.lineSpacing=1.05f;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;var shadow=go.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.72f);shadow.effectDistance=new Vector2(1,-1);var rt=t.rectTransform;rt.anchorMin=rt.anchorMax=parent==canvas.transform||parent==worldLayer?Vector2.zero:new Vector2(.5f,.5f);rt.anchoredPosition=pos;rt.sizeDelta=size;return t;}
        Image Bar(Transform parent,Vector2 pos,Vector2 size,Color fillColor,out Text label){var bg=Image("Bar",parent,chromeSprites[6],pos,size,new Color(1,1,1,.68f),false);var fill=Image("Fill",bg.transform,null,Vector2.zero,size-new Vector2(6,5),fillColor,false);fill.rectTransform.anchorMin=new Vector2(0,.5f);fill.rectTransform.anchorMax=new Vector2(0,.5f);fill.rectTransform.pivot=new Vector2(0,.5f);fill.rectTransform.anchoredPosition=new Vector2(3,0);label=Text("Value",bg.transform,"",Vector2.zero,size+new Vector2(0,8),12,Color.white,TextAnchor.MiddleCenter);return fill;}
    }
}
