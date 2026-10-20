using System;
using System.Linq;

namespace HarborCity
{
    public sealed partial class CityModel
    {
        public float FactoryProcessingRate(int id)=>development.enabled?1+.25f*(GetBuilding(id)?.level-1??0):1;
    }
}
