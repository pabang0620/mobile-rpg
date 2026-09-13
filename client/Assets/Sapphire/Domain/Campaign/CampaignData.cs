using System;
using System.Collections.Generic;

namespace Sapphire
{
    [Serializable] public sealed class EquipmentData { public string InstanceId; public string DefinitionId; public int Upgrade; public bool Equipped; public EquipmentData Copy() { return (EquipmentData)MemberwiseClone(); } }
    [Serializable] public sealed class QuestState { public string Id; public int Progress; public bool Accepted; public bool Claimed; public QuestState Copy() { return (QuestState)MemberwiseClone(); } }
    [Serializable] public sealed class ProfileData
    {
        public int SchemaVersion = 1; public string ProfileId; public long Revision; public int Level = 1; public int Xp; public int Gold = 60; public int HpPotions = 5; public int MpPotions = 5; public int Materials;
        public int QuestIndex; public bool BossDefeated; public string CheckpointZone = "town"; public string LastSettledRunId = ""; public float MusicVolume = .55f; public float SfxVolume = .7f;
        public List<EquipmentData> Inventory = new List<EquipmentData>(); public List<EquipmentData> RewardInbox = new List<EquipmentData>(); public List<QuestState> Quests = new List<QuestState>(); public List<string> RecentTransactions = new List<string>();
        public ProfileData Copy() { var p = (ProfileData)MemberwiseClone(); p.Inventory = Inventory.ConvertAll(x => x.Copy()); p.RewardInbox = RewardInbox.ConvertAll(x => x.Copy()); p.Quests = Quests.ConvertAll(x => x.Copy()); p.RecentTransactions = new List<string>(RecentTransactions); return p; }
    }
    public sealed class PlayerStats { public int MaxHp, MaxMp, Attack, Defense; }
    public sealed class ItemDefinition
    {
        public string Id, Name, Slot; public int Attack, Defense, Hp, Price;
        public ItemDefinition(string id, string name, string slot, int attack, int defense, int hp, int price) { Id=id; Name=name; Slot=slot; Attack=attack; Defense=defense; Hp=hp; Price=price; }
    }
    public sealed class QuestDefinition
    {
        public string Id, Name, Description, TargetKind; public int Target, Gold, Xp;
        public QuestDefinition(string id,string name,string description,string kind,int target,int gold,int xp) { Id=id; Name=name; Description=description; TargetKind=kind; Target=target; Gold=gold; Xp=xp; }
    }
    public static class CampaignCatalog
    {
        public static readonly int[] LevelXp = {80,120,180,260};
        public static readonly ItemDefinition[] Items = {
            new ItemDefinition("item.staff_01","수습생의 지팡이","Weapon",0,0,0,12), new ItemDefinition("item.staff_02","서리나무 지팡이","Weapon",6,0,0,40), new ItemDefinition("item.staff_03","균열의 홀","Weapon",12,0,0,80),
            new ItemDefinition("item.robe_01","수습생의 로브","Armor",0,0,0,12), new ItemDefinition("item.robe_02","별빛 로브","Armor",0,3,8,40), new ItemDefinition("item.robe_03","수호자의 로브","Armor",0,6,16,80),
            new ItemDefinition("item.charm_01","푸른 돌 부적","Charm",0,0,0,12), new ItemDefinition("item.charm_02","달빛 부적","Charm",2,1,10,40), new ItemDefinition("item.charm_03","사파이어 인장","Charm",4,2,20,80) };
        public static readonly QuestDefinition[] Quests = {
            new QuestDefinition("quest.q01","숲으로 향하는 첫걸음","의뢰를 수락하고 달빛 숲에 진입하세요.","travel",1,20,20),
            new QuestDefinition("quest.q02","숲길의 방해꾼","고블린 4마리를 처치하세요.","goblin",4,25,30),
            new QuestDefinition("quest.q03","흩어진 달빛","정령 또는 늑대에게 달빛 조각 3개를 모으세요.","material",3,30,40),
            new QuestDefinition("quest.q04","폐허의 문지기","던전 정예를 처치하고 원정을 완료하세요.","elite",1,40,45),
            new QuestDefinition("quest.q05","균열을 잠재우다","균열 수호자를 처치하고 원정을 완료하세요.","boss",1,60,60),
            new QuestDefinition("quest.q06","사파이어의 새벽","마을로 돌아와 첫 장의 보상을 받으세요.","return",1,80,80) };
        public static ItemDefinition Item(string id) { return Array.Find(Items,x=>x.Id==id); }
    }
    public interface IProfileStore { bool TryLoad(out ProfileData profile, out string warning); bool TrySave(ProfileData profile, out string error); }
}
