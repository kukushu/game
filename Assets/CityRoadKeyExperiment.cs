using System.Collections.Generic;
namespace HarborCity
{
    // Research control only: not used by normal builds. Equality preserves all 64 bits.
    internal sealed class CityRoadKeyExperiment : IEqualityComparer<long>
    {
        internal static readonly CityRoadKeyExperiment Instance=new CityRoadKeyExperiment();
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
