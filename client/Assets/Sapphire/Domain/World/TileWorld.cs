using System;
using System.Collections.Generic;

namespace Sapphire.World
{
    public enum Direction { Up, Down, Left, Right }
    public enum TileKind { Grass, Path, Wall, Tree, Water, Floor, Door }
    public enum MoveResult { Moved, Blocked, Warped }

    public struct TileCoord : IEquatable<TileCoord>
    {
        public readonly int X, Y;
        public TileCoord(int x, int y) { X = x; Y = y; }
        public TileCoord Step(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return new TileCoord(X, Y + 1);
                case Direction.Down: return new TileCoord(X, Y - 1);
                case Direction.Left: return new TileCoord(X - 1, Y);
                default: return new TileCoord(X + 1, Y);
            }
        }
        public bool Equals(TileCoord other) { return X == other.X && Y == other.Y; }
        public override bool Equals(object obj) { return obj is TileCoord && Equals((TileCoord)obj); }
        public override int GetHashCode() { unchecked { return X * 397 ^ Y; } }
        public override string ToString() { return X + "," + Y; }
        public static bool operator ==(TileCoord a, TileCoord b) { return a.Equals(b); }
        public static bool operator !=(TileCoord a, TileCoord b) { return !a.Equals(b); }
    }

    public sealed class WorldMap
    {
        private readonly TileKind[,] tiles;
        public readonly string Id;
        public int Width { get { return tiles.GetLength(0); } }
        public int Height { get { return tiles.GetLength(1); } }
        public WorldMap(string id, int width, int height, TileKind fill)
        {
            if (width < 1 || height < 1) throw new ArgumentOutOfRangeException();
            Id = id; tiles = new TileKind[width, height];
            for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) tiles[x, y] = fill;
        }
        public TileKind Get(TileCoord p) { return IsInside(p) ? tiles[p.X, p.Y] : TileKind.Wall; }
        public void Set(TileCoord p, TileKind kind) { if (!IsInside(p)) throw new ArgumentOutOfRangeException(); tiles[p.X, p.Y] = kind; }
        public bool IsInside(TileCoord p) { return p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height; }
        public bool IsPassable(TileCoord p) { TileKind k = Get(p); return k != TileKind.Wall && k != TileKind.Tree && k != TileKind.Water; }
    }

    public sealed class Warp
    {
        public readonly TileCoord At, Destination;
        public readonly string DestinationMap;
        public Warp(TileCoord at, string destinationMap, TileCoord destination) { At = at; DestinationMap = destinationMap; Destination = destination; }
    }

    public sealed class Npc
    {
        public readonly string Id, Name;
        public readonly TileCoord Position;
        public readonly Direction Facing;
        public readonly string Dialogue;
        public Npc(string id, string name, TileCoord position, Direction facing, string dialogue)
        { Id = id; Name = name; Position = position; Facing = facing; Dialogue = dialogue; }
    }

    public sealed class TileWorld
    {
        private readonly Dictionary<string, WorldMap> maps = new Dictionary<string, WorldMap>();
        private readonly Dictionary<string, List<Warp>> warps = new Dictionary<string, List<Warp>>();
        private readonly Dictionary<string, List<Npc>> npcs = new Dictionary<string, List<Npc>>();
        public string CurrentMapId { get; private set; }
        public TileCoord PlayerPosition { get; private set; }
        public Direction PlayerFacing { get; private set; }
        public WorldMap CurrentMap { get { return maps[CurrentMapId]; } }
        public TileWorld()
        {
            AddMap(CreateTown()); AddMap(CreateRoute()); AddMap(CreateHouse());
            AddWarp("town",new Warp(new TileCoord(11,3),"route",new TileCoord(1,4)));
            AddWarp("route",new Warp(new TileCoord(0,4),"town",new TileCoord(10,3)));
            AddWarp("town",new Warp(new TileCoord(5,6),"house",new TileCoord(3,1)));
            AddWarp("house",new Warp(new TileCoord(3,0),"town",new TileCoord(5,5)));
            AddNpc("town",new Npc("guide","루나",new TileCoord(7,4),Direction.Left,"사파이어 마을에 온 걸 환영해요! 동쪽 길은 달빛 숲으로 이어져요."));
            AddNpc("house",new Npc("elder","별빛 장로",new TileCoord(3,4),Direction.Down,"달빛 결정은 숲 가장 깊은 곳에서 빛난단다."));
            Enter("town", new TileCoord(3, 3), Direction.Down);
        }
        public WorldMap GetMap(string mapId) { return maps[mapId]; }
        public void AddMap(WorldMap map) { maps[map.Id] = map; if (!warps.ContainsKey(map.Id)) warps[map.Id] = new List<Warp>(); if (!npcs.ContainsKey(map.Id)) npcs[map.Id] = new List<Npc>(); }
        public void AddWarp(string mapId, Warp warp) { warps[mapId].Add(warp); }
        public void AddNpc(string mapId, Npc npc) { npcs[mapId].Add(npc); }
        public void Enter(string mapId, TileCoord position, Direction facing)
        { if (!maps.ContainsKey(mapId) || !maps[mapId].IsPassable(position)) throw new ArgumentException("Invalid map or spawn"); CurrentMapId = mapId; PlayerPosition = position; PlayerFacing = facing; }
        public MoveResult Move(Direction direction)
        {
            PlayerFacing = direction; TileCoord target = PlayerPosition.Step(direction);
            if (!CurrentMap.IsPassable(target) || IsNpcAt(target)) return MoveResult.Blocked;
            PlayerPosition = target;
            List<Warp> list = warps[CurrentMapId];
            for (int i = 0; i < list.Count; i++) if (list[i].At == target) { Warp w = list[i]; Enter(w.DestinationMap, w.Destination, direction); return MoveResult.Warped; }
            return MoveResult.Moved;
        }
        public MoveResult MovePlayer(Direction direction) { return Move(direction); }
        public Npc GetNpcAt(TileCoord position) { List<Npc> list = npcs[CurrentMapId]; for (int i = 0; i < list.Count; i++) if (list[i].Position == position) return list[i]; return null; }
        public string Interact()
        { Npc npc = GetNpcAt(PlayerPosition.Step(PlayerFacing)); return npc == null ? null : npc.Dialogue; }
        public bool IsNpcAt(TileCoord position) { return GetNpcAt(position) != null; }
        public IList<Npc> CurrentNpcs { get { return npcs[CurrentMapId].AsReadOnly(); } }

        public static WorldMap CreateTown()
        { WorldMap m = new WorldMap("town", 12, 9, TileKind.Grass); for (int x=0;x<12;x++){m.Set(new TileCoord(x,0),TileKind.Tree);m.Set(new TileCoord(x,8),TileKind.Tree);} for(int y=0;y<9;y++){m.Set(new TileCoord(0,y),TileKind.Tree);m.Set(new TileCoord(11,y),TileKind.Tree);} for(int x=1;x<12;x++)m.Set(new TileCoord(x,3),TileKind.Path); m.Set(new TileCoord(5,7),TileKind.Wall); m.Set(new TileCoord(5,6),TileKind.Door); return m; }
        public static WorldMap CreateRoute() { WorldMap m = new WorldMap("route", 14, 9, TileKind.Grass); for(int x=0;x<14;x++){m.Set(new TileCoord(x,0),TileKind.Tree);m.Set(new TileCoord(x,8),TileKind.Tree);} for(int y=1;y<8;y++){m.Set(new TileCoord(0,y),TileKind.Tree);m.Set(new TileCoord(13,y),TileKind.Tree);} for(int x=0;x<14;x++)m.Set(new TileCoord(x,4),TileKind.Path); for(int y=4;y<8;y++)m.Set(new TileCoord(6,y),TileKind.Path); m.Set(new TileCoord(0,4),TileKind.Door); return m; }
        public static WorldMap CreateHouse() { WorldMap m = new WorldMap("house", 7, 6, TileKind.Floor); for(int x=0;x<7;x++){m.Set(new TileCoord(x,0),TileKind.Wall);m.Set(new TileCoord(x,5),TileKind.Wall);} for(int y=0;y<6;y++){m.Set(new TileCoord(0,y),TileKind.Wall);m.Set(new TileCoord(6,y),TileKind.Wall);} m.Set(new TileCoord(3,0),TileKind.Door); return m; }
    }
}
