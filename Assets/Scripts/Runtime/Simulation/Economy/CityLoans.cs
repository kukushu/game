using System;
using System.Linq;
using System.Collections.Generic;

namespace HarborCity
{
    [Serializable] public sealed class CityLoan
    {
        public int id,offerId,takenDay,paid;
        public int principal,total,days;
    }
    [Serializable] public sealed class CityLoanState
    {
        public int nextId=1,lastSettledDay;
        public List<CityLoan> contracts=new List<CityLoan>();
    }
    public sealed partial class CityModel
    {
        public CityLoanState loans=new CityLoanState();
    }
}
