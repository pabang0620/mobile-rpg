using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace Sapphire
{
    public sealed class FileProfileStore : IProfileStore
    {
        readonly string path;
        [DataContract] sealed class Envelope { [DataMember] public string Payload; [DataMember] public string Sha256; }
        public string Path { get { return path; } }
        public FileProfileStore(string path) { this.path=System.IO.Path.GetFullPath(path); }
        public bool TryLoad(out ProfileData profile,out string warning)
        {
            profile=null;warning="";try{profile=Read(path);return true;}catch(Exception ex){warning="Primary save: "+ex.Message;}
            try{profile=Read(path+".bak");warning="Recovered backup. "+warning;return true;}catch(Exception ex){warning+="; Backup: "+ex.Message;return false;}
        }
        public bool TrySave(ProfileData profile,out string error)
        {
            error="";string temp=path+".tmp";
            try
            {
                Validate(profile);Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                string payload=Serialize(profile);var envelope=new Envelope{Payload=payload,Sha256=Hash(payload)};
                byte[] bytes=Encoding.UTF8.GetBytes(Serialize(envelope));using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                Read(temp);
                if(File.Exists(path))
                {
                    bool valid=true;try{Read(path);}catch{valid=false;}
                    if(!valid)ReplacePortable(temp,false);
                    else try { File.Replace(temp,path,path+".bak"); }
                    catch(UnauthorizedAccessException) { ReplacePortable(temp,valid); }
                    catch(PlatformNotSupportedException) { ReplacePortable(temp,valid); }
                }
                else File.Move(temp,path);
                return true;
            }
            catch(Exception ex){error=ex.GetType().Name+": "+ex.Message;return false;}
        }
        void ReplacePortable(string temp,bool preserveCurrent)
        {
            // Some Unity/Mono Windows runtimes deny File.Replace despite writable files.
            // Keep a verified backup before the short delete/move fallback window.
            if(preserveCurrent)File.Copy(path,path+".bak",true);
            File.Delete(path);File.Move(temp,path);
        }
        static ProfileData Read(string file)
        {
            var e=Deserialize<Envelope>(File.ReadAllText(file,Encoding.UTF8));if(e==null||e.Payload==null||e.Sha256!=Hash(e.Payload))throw new InvalidDataException("Checksum mismatch");
            var p=Deserialize<ProfileData>(e.Payload);Validate(p);return p;
        }
        static string Serialize<T>(T value) {using(var ms=new MemoryStream()){new DataContractJsonSerializer(typeof(T)).WriteObject(ms,value);return Encoding.UTF8.GetString(ms.ToArray());}}
        static T Deserialize<T>(string value) {using(var ms=new MemoryStream(Encoding.UTF8.GetBytes(value)))return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(ms);}
        static string Hash(string text) {using(var hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(text)));}
        public static void Validate(ProfileData p)
        {
            if(p==null||p.SchemaVersion!=1)throw new InvalidDataException("Unsupported save schema");
            if(string.IsNullOrEmpty(p.ProfileId)||p.Revision<0||p.Level<1||p.Level>5||p.Xp<0||p.Gold<0||p.HpPotions<0||p.HpPotions>99||p.MpPotions<0||p.MpPotions>99)throw new InvalidDataException("Invalid profile values");
            if(p.Level==5?p.Xp!=0:p.Xp>=CampaignCatalog.LevelXp[p.Level-1])throw new InvalidDataException("Invalid XP");
            if(p.Inventory==null||p.RewardInbox==null||p.Quests==null||p.RecentTransactions==null||p.Inventory.Count>24||p.RewardInbox.Count>24||p.Quests.Count!=CampaignCatalog.Quests.Length||p.QuestIndex<0||p.QuestIndex>6||p.RecentTransactions.Count>256)throw new InvalidDataException("Invalid collection");
            var ids=new System.Collections.Generic.HashSet<string>();var slots=new System.Collections.Generic.HashSet<string>();
            foreach(var item in p.Inventory){ValidateItem(item,ids);if(item.Equipped&&!slots.Add(CampaignCatalog.Item(item.DefinitionId).Slot))throw new InvalidDataException("Duplicate equipment slot");}
            foreach(var item in p.RewardInbox){ValidateItem(item,ids);if(item.Equipped)throw new InvalidDataException("Inbox equipment active");}
            for(int i=0;i<p.Quests.Count;i++){var q=p.Quests[i];if(q==null||q.Id!=CampaignCatalog.Quests[i].Id||q.Progress<0||q.Progress>CampaignCatalog.Quests[i].Target||q.Claimed!=(i<p.QuestIndex))throw new InvalidDataException("Invalid quest state");}
            if(float.IsNaN(p.MusicVolume)||float.IsNaN(p.SfxVolume)||p.MusicVolume<0||p.MusicVolume>1||p.SfxVolume<0||p.SfxVolume>1)throw new InvalidDataException("Invalid settings");
        }
        static void ValidateItem(EquipmentData item,System.Collections.Generic.HashSet<string> ids) {if(item==null||string.IsNullOrEmpty(item.InstanceId)||!ids.Add(item.InstanceId)||CampaignCatalog.Item(item.DefinitionId)==null||item.Upgrade<0||item.Upgrade>3)throw new InvalidDataException("Invalid equipment");}
    }
}
