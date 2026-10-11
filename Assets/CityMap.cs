using System;

namespace HarborCity
{
    // One supported map profile for now; persist it rather than infer it from city size.
    [Serializable] public sealed class CityMapState
    {
        public const float TerrainHalfSize=240f, BuildHalfSize=168f;
        public const int TerrainResolution=1025;
        public int terrainVersion=2;
        public float terrainHalfSize=TerrainHalfSize, buildHalfSize=BuildHalfSize;
        public bool Valid()=>terrainVersion==2 && terrainHalfSize==TerrainHalfSize && buildHalfSize==BuildHalfSize;
    }
}
