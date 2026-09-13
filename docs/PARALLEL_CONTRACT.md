# Parallel implementation contract v1

Latest user authorizes implementation and parallel agents. Product is top-down 2D original sapphire mage adventure. Root integrates module boundaries; each implementation exposes a small API and sends exact signatures promptly.

Combat owns Vec2 (X,Y floats; constructor, arithmetic, Length, Normalized), CombatWorld, CombatActor, CombatProjectile, CombatEvent. Public CombatWorld accepts zoneId and player stats, has player/enemies/projectiles/events, Step(Vec2 movement), Attack(), Cast(int slot), Dodge(), Reset/EnterZone. Pure C# simulation. Agent may refine names but must send API before UI wiring. Tick 60Hz, bounds about 20x11 units.

Campaign owns CampaignService, ProfileData, IProfileStore/FileProfileStore, inventory/quests/transactions/dungeon. No Unity types. API can be designed by owner but report promptly. Root coordinates combat kill events to campaign grants and scene travel. Persist field rewards, defer dungeon rewards, preserve potion consumption.

Root owns GameApp MonoBehaviour facade. Presentation MUST depend only on this facade, not directly on campaign/combat implementation. Root will provide public fields/properties below. Presentation uses GameApp.Instance (created by Bootstrap) and public calls. All positions normalized to a 20x11 XY world, y up.

GameApp fields/properties:
- string Mode ("Title","Town","Field","Dungeon","Result"), ZoneName, Objective, Notice, SaveStatus;
- int Level, Hp, MaxHp, Mp, MaxMp, Gold, Xp, NextXp, HpPotions, MpPotions;
- bool Paused, Auto, Dead, CanContinue;
- float PlayerX, PlayerY, FacingX, FacingY, AttackProgress, DodgeRemaining, ShieldRemaining;
- float[] Cooldowns, MaxCooldowns; bool[] SkillUnlocked;
- List<ActorView> Actors; List<ProjectileView> Projectiles; List<EffectView> Effects;
- List<ItemView> Inventory; List<QuestView> Quests;
- string[] DestinationNames; int QuestIndex;
- float MusicVolume, SfxVolume.

GameApp methods: NewGame(), ContinueGame(), Move(float x,float y), Attack(), Cast(int slot), Dodge(), UsePotion(bool hp), ToggleAuto(), Interact(), Travel(int destination), ReturnTown(), Respawn(), SetPaused(bool), Equip(string instanceId), Upgrade(string instanceId), Sell(string instanceId), BuyPotion(bool hp), AcceptQuest(), TurnInQuest(), SaveNow(), SetVolumes(float music,float sfx).

ActorView public fields: int Id, Hp, MaxHp; string Kind, Name; float X,Y,FacingX,FacingY, Telegraph, TelegraphRadius; bool Dead, Boss; float Flash.
ProjectileView fields: float X,Y,DirectionX,DirectionY; bool Hostile.
EffectView fields: float X,Y,Age; string Kind; int Amount.
ItemView fields: string Id,Name,Description,Slot; int Upgrade,Price; bool Equipped.
QuestView fields: string Name,Description,Status; int Progress,Target.

All facade properties are readable, gameplay changes only through methods. Presentation builds uGUI keyboard/touch, camera-backed canvas for capture, world background and sorted character sprites. GameApp owns time/commands and presentation owns menus/labels only. Dynamic Korean uses copied Noto font. Art initial names: MoonshardField, SapphireTown, MageIdle, MagePose02/06/09/14, Goblin, MageSkills, MainMenuIcons, HudControls, UiChrome, WideButton, MagePortrait. New generated assets will be communicated. Use serialized Texture2D fields populated in Editor builder, no filesystem lookup in player.

Build namespace Sapphire.Editor.BuildGame; static BuildAndTest should generate Boot scene and Windows player under project builds/Windows/SapphireRPG.exe. Root invokes Unity. Editor can discover static module test RunAll methods once provided; coordinate names. Player supports --smoke and --capture-dir; root implements automated gameplay scenario, presentation exposes camera capture helper if useful.
