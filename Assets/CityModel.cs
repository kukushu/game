using System;
using System.Collections.Generic;

namespace HarborCity
{
    public enum LandUse { Empty, Road, Residential, Commercial, Industrial, Power, Water, Park, Bulldoze }

    [Serializable]
    public sealed partial class CityModel
    {
        public const int Size = 36;
        public int version = 1;
        public int money = 65000;
        public int day = 1;
        public int[] tiles = new int[Size * Size];
        public int[] levels = new int[Size * Size];
        public TrafficState traffic;
        public CityRoads roads;
        [NonSerialized] public bool[] connected = new bool[Size * Size];
        [NonSerialized] public int population, jobs, income, upkeep, power, water, demand;
        [NonSerialized] public int happiness = 70;

        public static int Index(int x, int z) => z * Size + x;
        public static bool Inside(int x, int z) => x >= 0 && z >= 0 && x < Size && z < Size;
        public LandUse Get(int x, int z) => Inside(x, z) ? (LandUse)tiles[Index(x, z)] : LandUse.Empty;
        public static int Cost(LandUse use)
        {
            switch (use)
            {
                case LandUse.Road: return 100;
                case LandUse.Residential: case LandUse.Commercial: case LandUse.Industrial: return 60;
                case LandUse.Power: return 3500;
                case LandUse.Water: return 2500;
                case LandUse.Park: return 500;
                case LandUse.Bulldoze: return 40;
                default: return 0;
            }
        }

        public bool Place(int x, int z, LandUse use, out string message)
        {
            message = "";
            if (version >= 3) { message = "请使用沿路地块工具"; return false; }
            if (!Inside(x, z) || use == LandUse.Empty) return false;
            if (roads != null && use == LandUse.Road) { message = "请使用自由道路工具"; return false; }
            int i = Index(x, z);
            if (x == 0 && z == Size / 2) { message = "保留西侧城市入口道路"; return false; }
            if (use == LandUse.Bulldoze && tiles[i] == 0) return false;
            if (use != LandUse.Bulldoze && tiles[i] != 0) { message = "请先拆除已有设施"; return false; }
            if (use != LandUse.Bulldoze && roads != null && roads.OverlapsLot(i)) { message = "地块与道路重叠，请移到道路两侧"; return false; }
            if (money < Cost(use)) { message = "资金不足，请等待税收或减少支出"; return false; }
            money -= Cost(use);
            tiles[i] = use == LandUse.Bulldoze ? 0 : (int)use;
            levels[i] = 0;
            Recalculate();
            return true;
        }

        public bool RoadAccess(int x, int z)
        {
            if (version >= 3) return BuildingAccess(Index(x,z));
            if (roads != null) return Inside(x,z) && roads.LotAccess(Index(x,z));
            return IsConnected(x - 1, z) || IsConnected(x + 1, z) || IsConnected(x, z - 1) || IsConnected(x, z + 1);
        }

        bool IsConnected(int x, int z) => Inside(x, z) && connected[Index(x, z)];
        public bool EntityRoadAccess(int id) => version >= 3 ? BuildingAccess(id) : RoadAccess(id % Size,id / Size);

        public void Recalculate()
        {
            if(version>=4 && society!=null) {RecalculateHouseholds(); return;}
            if (connected == null || connected.Length != tiles.Length) connected = new bool[tiles.Length];
            Array.Clear(connected, 0, connected.Length);
            var queue = new Queue<int>();
            int entrance = Index(0, Size / 2);
            if (tiles[entrance] == (int)LandUse.Road) { connected[entrance] = true; queue.Enqueue(entrance); }
            while (queue.Count > 0)
            {
                int i = queue.Dequeue(), x = i % Size, z = i / Size;
                Visit(x - 1, z, queue); Visit(x + 1, z, queue); Visit(x, z - 1, queue); Visit(x, z + 1, queue);
            }
            population = jobs = income = upkeep = power = water = demand = 0;
            if (roads != null)
            {
                float length = 0;
                foreach (var edge in roads.edges) length += roads.EdgeLength(edge.a,edge.b);
                upkeep = (int)Math.Ceiling(length / 3);
            }
            int parks = 0, pollution = 0;
            for (int i = 0; i < tiles.Length; i++)
            {
                int level = levels[i];
                var use = (LandUse)tiles[i];
                bool access = EntityRoadAccess(i);
                if (use == LandUse.Road) upkeep += 1;
                if (use == LandUse.Power) { upkeep += 90; if (access) power += 160; }
                if (use == LandUse.Water) { upkeep += 65; if (access) water += 160; }
                if (use == LandUse.Park) { upkeep += 8; if (access) parks++; }
                if (level == 0) continue;
                demand += level;
                if (!access) continue;
                if (use == LandUse.Residential) { population += level * 12; income += level * 18; }
                if (use == LandUse.Commercial) { jobs += level * 8; income += level * 25; }
                if (use == LandUse.Industrial) { jobs += level * 12; income += level * 30; pollution += level; }
            }
            happiness = Math.Clamp(65 + parks * 4 - pollution / 3 - Math.Max(0, population / 2 - jobs) / 8
                - (demand > power ? 25 : 0) - (demand > water ? 25 : 0), 10, 100);
            if (power < demand || water < demand) income /= 2;
        }

        void Visit(int x, int z, Queue<int> queue)
        {
            if (!Inside(x, z)) return;
            int i = Index(x, z);
            if (connected[i] || tiles[i] != (int)LandUse.Road) return;
            connected[i] = true; queue.Enqueue(i);
        }

        public List<int> Tick()
        {
            if(version>=4 && society!=null) {HouseholdTick(); return new List<int>();}
            day++;
            Recalculate();
            var changes = new List<int>();
            for (int i = 0; i < tiles.Length; i++)
            {
                var use = (LandUse)tiles[i];
                if (use < LandUse.Residential || use > LandUse.Industrial) continue;
                if (!EntityRoadAccess(i) || power <= demand || water <= demand) continue;
                if (levels[i] >= 3 || (i + day) % 5 != 0) continue;
                if (use == LandUse.Residential && population > jobs * 3 + 100) continue;
                if (use != LandUse.Residential && jobs > population + 40) continue;
                levels[i]++; demand++; changes.Add(i);
            }
            Recalculate();
            money += income - upkeep;
            return changes;
        }

        public void EnableRoads(Func<float,float,float> height)
        {
            if (version>=2 && roads != null) return;
            roads = CityRoads.FromGrid(this,height);
            for (int i = 0; i < tiles.Length; i++) if (tiles[i] == (int)LandUse.Road) tiles[i] = 0;
            version = 2; Recalculate();
        }

        public bool CommitRoad(RoadPlan plan)
        {
            if (roads == null || !plan.Valid || plan.revision != roads.revision || money < plan.cost) return false;
            roads = plan.network; roads.ApplySplits(traffic,plan.splits); money -= plan.cost; Recalculate(); return true;
        }

        public bool Valid() => (version >= 1 && version <= 4) && tiles != null && levels != null && (version >= 3 ? ValidBuildings() : tiles.Length == Size * Size)
            && levels.Length == tiles.Length && day > 0 && Array.TrueForAll(tiles, t => t >= 0 && t <= (int)LandUse.Park)
            && Array.TrueForAll(levels, l => l >= 0 && l <= 3)
            && (version == 1 ? (roads == null || (roads.nodes!=null && roads.nodes.Count==0 && roads.edges!=null && roads.edges.Count==0)) && tiles[Index(0, Size / 2)] == (int)LandUse.Road : roads != null && roads.Valid())
            && CityTraffic.Valid(traffic,version==1?null:roads,tiles.Length) && (version<4 || ValidHouseholds());

        public static CityModel Create()
        {
            var city = new CityModel();
            for (int x = 0; x <= 26; x++) city.tiles[Index(x, 18)] = (int)LandUse.Road;
            for (int z = 10; z <= 26; z++) { city.tiles[Index(12, z)] = 1; city.tiles[Index(24, z)] = 1; }
            for (int x = 12; x <= 24; x++) { city.tiles[Index(x, 10)] = 1; city.tiles[Index(x, 26)] = 1; }
            city.tiles[Index(5, 19)] = (int)LandUse.Power;
            city.tiles[Index(7, 19)] = (int)LandUse.Water;
            for (int x = 13; x <= 22; x++)
            {
                city.tiles[Index(x, 17)] = (int)LandUse.Residential;
                city.tiles[Index(x, 19)] = (int)LandUse.Commercial;
                city.levels[Index(x, 17)] = 1 + x % 2;
                city.levels[Index(x, 19)] = 1;
            }
            for (int z = 11; z < 17; z++) { city.tiles[Index(25, z)] = 4; city.levels[Index(25, z)] = 1; }
            city.tiles[Index(15, 11)] = (int)LandUse.Park;
            city.Recalculate();
            return city;
        }
    }
}
