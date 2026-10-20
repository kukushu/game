using System;

namespace HarborCity
{
    // Persisted map bounds and terrain profile are authoritative. Version 2 retains
    // the original hills; version 3 is flat with explicit persisted bounds.
    [Serializable] public sealed class CityMapState
    {
        public const float TerrainHalfSize=240f, BuildHalfSize=168f;
        public const float GameplayBuildHalfSize=672f;
        public const int TerrainResolution=1025;
        public int terrainVersion=2;
        public float terrainHalfSize=TerrainHalfSize, buildHalfSize=BuildHalfSize;
        public bool Valid()=>terrainVersion==2 && terrainHalfSize==TerrainHalfSize && buildHalfSize==BuildHalfSize
            || terrainVersion==3 && !float.IsNaN(buildHalfSize) && !float.IsInfinity(buildHalfSize) && buildHalfSize>=BuildHalfSize && buildHalfSize<=2048 && terrainHalfSize==buildHalfSize+72;
        public static CityMapState DiagnosticFlat(float buildExtent)
        {var map=new CityMapState{terrainVersion=3,buildHalfSize=buildExtent,terrainHalfSize=buildExtent+72};if(!map.Valid())throw new ArgumentOutOfRangeException(nameof(buildExtent));return map;}
        // Expand flat player cities without moving any existing world content.
        // Keep captured diagnostic inputs and original hill profiles unchanged.
        public void ExpandForPlay()
        {
            if(!Valid())throw new InvalidOperationException("Invalid map bounds");
            if(terrainVersion!=3||buildHalfSize>=GameplayBuildHalfSize)return;
            buildHalfSize=GameplayBuildHalfSize;terrainHalfSize=buildHalfSize+72;
        }
    }
}
