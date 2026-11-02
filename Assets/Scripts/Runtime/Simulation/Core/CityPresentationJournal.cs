using System.Collections.Generic;
namespace HarborCity
{
    // Optional read-side invalidations. No callbacks, coordinates or business rules.
    // A single presentation owner drains after simulation; nothing is saved.
    internal sealed class CityPresentationJournal
    {
        internal readonly List<int> Ids=new List<int>();
        internal long Epoch=1;
        readonly HashSet<int> unique=new HashSet<int>();
        internal void Mark(int id){if(unique.Add(id))Ids.Add(id);}
        internal void Vehicle(int id,ref long stamp){if(stamp==Epoch)return;stamp=Epoch;Ids.Add(id);}
        internal void Clear(){Ids.Clear();unique.Clear();Epoch++;}
    }
}
