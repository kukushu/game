using System;
using System.Collections.Generic;
using System.Linq;

namespace HarborCity
{
    public enum HousingKind { Apartment, Villa }
    [Serializable] public sealed class Household
    {
        public int id, unit=-1, savings=600;
        int address=-1;bool active;
        internal CityEntities EntityOwner;
        [CitySavedFact] public int home {get=>address;set{if(address==value)return;int previous=address;address=value;EntityOwner?.HouseholdHomeChanged(this,previous);}}
        [CitySavedFact] public bool resident {get=>active;set{if(active==value)return;active=value;EntityOwner?.HouseholdChanged(this,CityResidentStateChange.Residence);}}
        public int rent, leaseEnd, lastMove=-100, nextReview, hardship, arrears;
        public float spacePreference, privacyPreference, timePreference, savingPreference;
        public float noiseSensitivity, pollutionSensitivity, trafficSensitivity;
        public int wagePaid, rentPaid, livingPaid, travelPaid;
        public int evaluatedDay,shoppingDay=-1;
        public int openingSavings,retailPaid;
        public long incomeReceived,taxPaid;
        public string reason="等待住房和工作";
        public List<HouseholdOption> options=new List<HouseholdOption>();
        public List<CityResident> people=new List<CityResident>();
    }
    [Serializable] public sealed class HouseholdOption
    {
        public int home, rent, wage, surplus;
        public List<int> citizenIds=new List<int>(), jobIds=new List<int>();
        public float minutes, spaceScore, privacyScore, moneyScore, timeScore, changeCost, score;
        public float noise, pollution, heavyTraffic, environmentCost;
        public string rejection="";
    }
    [Serializable] public sealed class HouseholdDay
    {
        public int unemployed;
        public int tax;
        public int loanPayment;
        public int factoryWages, externalWages;
        public int day, households, population, units, employed, wages, rent, living, travel, maintenance, moved, arrived, left;
        public int openingTreasury, closingTreasury, openingSavings, closingSavings, movingCosts, unpaidRent;
        public float averageCommute;
    }
    [Serializable] public sealed class HouseholdSettings
    {
        public float secondsPerDay=120;
        public int reviewDays=7, leaseDays=30, minimumStay=7, moveCost=80, applicantsPerDay=2;
        public int apartmentCost=6000, villaCost=2800, apartmentMaintenance=24, villaMaintenance=12;
        public int apartmentRent=24, villaRent=55;
        public float improvement=8;
    }
    [Serializable] public sealed class HouseholdState
    {
        // Technical state/resource guard, not a migration or economy tuning rule.
        public const int MaximumFamilies=100000;
        public int nextId=1, seed=19237, lastSettledDay;
        public float dayElapsed;
        public int nextJobId=1;
        public int nextCitizenId;
        public long retiredWagesPaid;
        public List<CityJob> jobEntities=new List<CityJob>();
        public HouseholdSettings settings=new HouseholdSettings();
        public List<Household> families=new List<Household>();
        public List<HouseholdDay> history=new List<HouseholdDay>();
        public List<string> events=new List<string>();
    }

    public sealed partial class CityModel
    {
        public HouseholdState society=new HouseholdState {lastSettledDay=1,nextCitizenId=10};
        public int HousingCapacity(int id) => Residence(id)==null || Residence(id).abandoned?0:Residence(id).housingUnits;
        public int Occupancy(int id) => HousingCapacity(id)==0?0:society.families.Count(h=>h.resident && h.home==id && h.unit>=0 && h.unit<HousingCapacity(id));
        public int JobCapacity(int id) => society.jobEntities.Count(j=>j.buildingId==id);
        public int EmployedAt(int id) => society.jobEntities.Count(j=>j.buildingId==id && j.occupiedCitizenId>=0);
    }
}
