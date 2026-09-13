using Sapphire.World;
using System;

namespace Sapphire.Tests
{
    public static class WorldChecks
    {
        static void Check(bool value,string message){if(!value)throw new Exception("WorldChecks: "+message);}
        public static void RunAll()
        {
            TileWorld w = new TileWorld(); w.Enter("town", new TileCoord(1, 1), Direction.Up); Check(w.Move(Direction.Left)==MoveResult.Blocked,"tree collision");Check(w.PlayerPosition==new TileCoord(1,1),"blocked position");
            w.Enter("town",new TileCoord(10,3),Direction.Right);Check(w.Move(Direction.Right)==MoveResult.Warped,"route warp");Check(w.CurrentMapId=="route"&&w.PlayerPosition==new TileCoord(1,4),"route destination");
            w.Enter("town",new TileCoord(6,4),Direction.Right);Check(w.Move(Direction.Right)==MoveResult.Blocked,"npc collision");Check(!string.IsNullOrEmpty(w.Interact()),"npc dialogue");
        }
    }
}
