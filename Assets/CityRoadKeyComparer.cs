using System.Collections.Generic;
namespace HarborCity
{
    // Default for derived road lookups in Editor and every Player build.
    // Packed adjacent IDs collide under Int64's XOR hash; equality still uses all 64 bits.
    internal sealed class CityRoadKeyComparer : IEqualityComparer<long>
    {
        internal static readonly CityRoadKeyComparer Instance=new CityRoadKeyComparer();
        CityRoadKeyComparer() {}
        public bool Equals(long x,long y)=>x==y;
        public int GetHashCode(long key)
        {
            unchecked
            {
                ulong z=(ulong)key;
                z=(z^(z>>30))*0xbf58476d1ce4e5b9UL;
                z=(z^(z>>27))*0x94d049bb133111ebUL;
                z^=z>>31;return (int)(z^(z>>32));
            }
        }
    }
}
