using System;

namespace HarborCity
{
    // Persisted map bounds and terrain profile are authoritative. Version 2 retains
    // the original hills; version 3 is flat, also used by new games at original bounds.
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
