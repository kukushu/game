using System;
namespace HarborCity
{
    // Only the core city's actual attendance phase may debit employers. Wage
    // credit is owed to a citizen until the existing daily settlement transfers
    // whole units to household savings; fractional credit is retained.
    public sealed class CityCorePayroll
    {
        readonly CityModel city;readonly CityEntities entities;readonly EmploymentRules rules;
        public int PaidDay {get;private set;}
        readonly System.Collections.Generic.Dictionary<int,int> staffed=new System.Collections.Generic.Dictionary<int,int>();
        internal CityCorePayroll(CityModel city,CityEntities entities,EmploymentRules rules,int paidDay=-1){this.city=city;this.entities=entities;this.rules=rules;PaidDay=paidDay<0?city.day:paidDay;if(PaidDay<1||PaidDay>city.day)throw new ArgumentException("Invalid saved payroll day");}
        internal void BeginStep()=>staffed.Clear();
        public int WorkersAt(int building)=>staffed.TryGetValue(building,out int count)?count:0;
        internal void SettleDay()
        {
            if(city.day<=PaidDay)return;
            // Validate the complete daily transfer before changing any account.
            foreach(var family in city.society.families){double total=0;foreach(var p in family.people)total+=Math.Floor(p.wageCredit)+Math.Round(p.earnedWages);if(total<0||total>int.MaxValue-family.savings||total>long.MaxValue-family.incomeReceived)throw new InvalidOperationException("Household balance exceeds supported whole-currency range");}
            foreach(var family in city.society.families)family.wagePaid=0;
            foreach(var member in entities.Members)
            {
                var p=member.Person;double amount=Math.Floor(p.wageCredit)+Math.Round(p.earnedWages);
                if(amount>int.MaxValue-member.Family.savings)throw new InvalidOperationException("Household balance exceeds supported whole-currency range");
                int paid=city.TransferEmploymentPay(p);p.wagePaid=paid+(int)Math.Round(p.earnedWages);member.Family.savings+=p.wagePaid;member.Family.wagePaid+=p.wagePaid;member.Family.incomeReceived+=p.wagePaid;p.earnedWages=0;p.workedMinutes=0;
            }
            PaidDay=city.day;
        }
        internal float PayAttendance(CityResident person,float minutes)
        {
            if(minutes<=0)return 0;var job=city.ResidentJob(person);if(job==null)return 0;
            double due=minutes/(double)rules.ShiftMinutes*job.wage,paid=0;
            var employer=entities.Building(job.buildingId);
            if(employer is IndustrialBuilding factory){var account=factory.factory;paid=Math.Min(account.cash,due);account.cash-=paid;account.wageCosts+=paid;account.unpaidWages+=due-paid;}
            else if(employer is CommercialBuilding shop){var account=shop.Account;paid=Math.Min(account.Cash,due);account.Cash-=paid;account.WageCosts+=paid;account.UnpaidWages+=due-paid;}
            person.wageCredit+=paid;if(paid>0||due==0){staffed.TryGetValue(job.buildingId,out int n);staffed[job.buildingId]=n+1;}return due==0?minutes:(float)(minutes*paid/due);
        }
    }
}
