using System;
using System.Linq;

namespace HarborCity
{
    public sealed partial class CityModel
    {
        public bool CanAttendWork(CityResident p)=>p!=null && p.canWork && !p.sick;
    }
}
