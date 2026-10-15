using System;

namespace HarborCity
{
    // Persisted map bounds are authoritative. Version 3 is an explicit large flat
    // diagnostic profile; normal new games still use version 2 and original bounds.
    [Serializable] public sealed class CityMapState
    {
        public const float TerrainHalfSize=240f, BuildHalfSize=168f;
        public const int TerrainResolution=1025;
        public int terrainVersion=2;
        public float terrainHalfSize=TerrainHalfSize, buildHalfSize=BuildHalfSize;
        public bool Valid()=>terrainVersion==2 && terrainHalfSize==TerrainHalfSize && buildHalfSize==BuildHalfSize
            || terrainVersion==3 && !float.IsNaN(buildHalfSize) && !float.IsInfinity(buildHalfSize) && buildHalfSize>=BuildHalfSize && buildHalfSize<=2048 && terrainHalfSize==buildHalfSize+72;
        public static CityMapState DiagnosticFlat(float buildExtent)
        {var map=new CityMapState{terrainVersion=3,buildHalfSize=buildExtent,terrainHalfSize=buildExtent+72};if(!map.Valid())throw new ArgumentOutOfRangeException(nameof(buildExtent));return map;}
    }
}
