using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Fixer {
    static void Main() {
        // Because the current files on disk are already corrupted with ?, we must checkout the old commit to grab the real text,
        // or since we know the text, we can checkout HEAD, which has the ? corrupted text, but we need to overwrite it with UTF8!
        
        string file = @"client\Assets\Sapphire\Editor\SlimeKingdomTerrainBuilder.cs";
        string text = File.ReadAllText(file, Encoding.UTF8); // The file might be corrupted, but we will regex replace it!
        
        text = Regex.Replace(text, @"BuildEncounter\(root, collision, blocker, zones, 16, 13, "".*?""\);", @"BuildEncounter(root, collision, blocker, zones, 16, 13, ""야생 슬라임이 길을 막고 있습니다."");");
        text = Regex.Replace(text, @"BuildEncounter\(root, collision, blocker, zones, 14, 18, "".*?""\);", @"BuildEncounter(root, collision, blocker, zones, 14, 18, ""수풀 근처에서 서성이는 슬라임 무리입니다."");");
        text = Regex.Replace(text, @"BuildEncounter\(root, collision, blocker, zones, 25, 18, "".*?""\);", @"BuildEncounter(root, collision, blocker, zones, 25, 18, ""마력을 머금은 변종 슬라임이 경계하고 있습니다."");");
        text = Regex.Replace(text, @"BuildEncounter\(root, collision, blocker, zones, 19, 25, "".*?"", true\);", @"BuildEncounter(root, collision, blocker, zones, 19, 25, ""보스!"", true);");
        
        text = Regex.Replace(text, @"BuildInteractable\(root, collision, blocker, zones, PrimaryAtlas, ""return_gate"", ""SlimeProp_Gate"", 19, 0, "".*?"", ""VillageHub"", 2\.5f\);", @"BuildInteractable(root, collision, blocker, zones, PrimaryAtlas, ""return_gate"", ""SlimeProp_Gate"", 19, 0, ""시작 마을(사파이어 허브)로 돌아갑니다."", ""VillageHub"", 2.5f);");
        
        text = Regex.Replace(text, @"BuildInteractable\(root, collision, blocker, zones, PropsAtlas, ""entrance_sign"", ""Slime2_Sign"", 16, 4, "".*?"", null, 1\.05f\);", @"BuildInteractable(root, collision, blocker, zones, PropsAtlas, ""entrance_sign"", ""Slime2_Sign"", 16, 4, ""숲 깊은 곳으로 진입합니다. 몬스터에 주의하세요"", null, 1.05f);");
        text = Regex.Replace(text, @"BuildInteractable\(root, collision, blocker, zones, PrimaryAtlas, ""west_chest"", ""SlimeProp_Chest"", 12, 19, "".*?"", null, 2f\);", @"BuildInteractable(root, collision, blocker, zones, PrimaryAtlas, ""west_chest"", ""SlimeProp_Chest"", 12, 19, ""낡은 나무 상자입니다. 텅 비어 있습니다."", null, 2f);");
        text = Regex.Replace(text, @"BuildInteractable\(root, collision, blocker, zones, PrimaryAtlas, ""east_chest"", ""SlimeProp_Chest"", 27, 19, "".*?"", null, 1\.15f\);", @"BuildInteractable(root, collision, blocker, zones, PrimaryAtlas, ""east_chest"", ""SlimeProp_Chest"", 27, 19, ""녹슨 철제 상자입니다. 열리지 않습니다."", null, 1.15f);");
        text = Regex.Replace(text, @"BuildInteractable\(root, collision, blocker, zones, PropsAtlas, ""crystal_cave"", ""Slime2_Cave"", 3, 21, "".*?"", null, 1\.15f\);", @"BuildInteractable(root, collision, blocker, zones, PropsAtlas, ""crystal_cave"", ""Slime2_Cave"", 3, 21, ""어두운 동굴 입구입니다. 아직 진입할 수 없습니다."", null, 1.15f);");
        
        File.WriteAllText(file, text, new UTF8Encoding(false)); // Without BOM to be safe, Unity handles both

        file = @"client\Assets\Sapphire\Editor\VillageHubUiBuilder.cs";
        text = File.ReadAllText(file, Encoding.UTF8);
        text = Regex.Replace(text, @"string regionName = "".*?""\)", @"string regionName = ""사파이어 광장"")");
        text = Regex.Replace(text, @"buttonText\.text = "".*?"";", @"buttonText.text = ""닫기"";");
        text = Regex.Replace(text, @"// reserves the SSOT's "".*?"" slot", @"// reserves the SSOT's ""지역/보스 HP"" slot");
        File.WriteAllText(file, text, new UTF8Encoding(false));
        
        file = @"client\Assets\Sapphire\Editor\LoginSceneBuilder.cs";
        text = File.ReadAllText(file, Encoding.UTF8);
        text = Regex.Replace(text, @"placeholder\.text = "".*?"";", @"placeholder.text = ""아이디 입력"";");
        text = Regex.Replace(text, @"new Vector2\(280f, 90f\), "".*?"", fontSize: 28\);", @"new Vector2(280f, 90f), ""게임 시작"", fontSize: 28);");
        File.WriteAllText(file, text, new UTF8Encoding(false));
        
        file = @"client\Assets\Sapphire\Editor\SapphireSceneBuilder.cs";
        text = File.ReadAllText(file, Encoding.UTF8);
        text = Regex.Replace(text, @"Debug\.LogWarning\("".*?""\);", @"Debug.LogWarning(""SapphireSceneBuilder.BuildAll()은 씬을 덮어쓰기 때문에 에디터에서만 실행해야 합니다."");");
        File.WriteAllText(file, text, new UTF8Encoding(false));
    }
}
