using System;
using System.Linq;
using System.Collections.Generic;

namespace HarborCity
{
    public sealed class CityLoanOffer
    {
        public readonly int id,principal,interestPercent,weeks;
        public readonly string name;
        public int Total=>principal+principal*interestPercent/100;
        public int Days=>weeks*7;
        public CityLoanOffer(int id,string name,int principal,int interestPercent,int weeks)
        {this.id=id;this.name=name;this.principal=principal;this.interestPercent=interestPercent;this.weeks=weeks;}
    }
    [Serializable] public sealed class CityLoan
    {
        public int id,offerId,takenDay,paid;
    }
    [Serializable] public sealed class CityLoanState
    {
        public int nextId=1,lastSettledDay;
        public List<CityLoan> contracts=new List<CityLoan>();
    }
    public sealed partial class CityModel
    {
        public CityLoanState loans=new CityLoanState();
        // CS1 published catalogue. Calendar pacing and early-payoff calculation
        // still need original-game comparison; current simulation settles daily.
        public static readonly IReadOnlyList<CityLoanOffer> LoanOffers=Array.AsReadOnly(new[]{
            new CityLoanOffer(0,"银色夕阳银行",20000,5,52),
            new CityLoanOffer(1,"全球信贷公司",60000,10,260),
            new CityLoanOffer(2,"金字塔投资",200000,15,520)});
        public static CityLoanOffer LoanOffer(int id)=>id>=0 && id<LoanOffers.Count?LoanOffers[id]:null;
        public int LoanRemaining(CityLoan loan)=>LoanOffer(loan.offerId).Total-loan.paid;
        public CityLoan ActiveLoan(int offer)=>loans.contracts.FirstOrDefault(l=>l.offerId==offer && LoanRemaining(l)>0);
        public int LoanDebt=>loans.contracts.Sum(LoanRemaining);
        int ScheduledPaid(CityLoan loan,int onDay)
        {var offer=LoanOffer(loan.offerId);return (int)((long)offer.Total*Math.Min(offer.Days,Math.Max(0,onDay-loan.takenDay))/offer.Days);}
        public int LoanPaymentNextDay=>loans.contracts.Sum(l=>Math.Max(0,ScheduledPaid(l,day+1)-l.paid));
        public int LastLoanPayment=>society.history.Count==0?0:society.history[society.history.Count-1].loanPayment;
        public int NetCityIncome=>income-upkeep-LoanPaymentNextDay;
        public bool TakeLoan(int offerId,out string reason)
        {
            var offer=LoanOffer(offerId);
            if(!development.enabled || offer==null){reason="当前模式或贷款类型不可用。";return false;}
            if(ActiveLoan(offerId)!=null){reason="这笔贷款尚未还清，不能重复借入。";return false;}
            if(loans.contracts.Count>=10000 || loans.nextId>=int.MaxValue-1 || (long)money+offer.principal>int.MaxValue){reason="贷款记录或资金达到上限。";return false;}
            var loan=new CityLoan{id=loans.nextId++,offerId=offerId,takenDay=day};
            loans.contracts.Add(loan);money+=offer.principal;
            reason="贷款 ¥"+offer.principal.ToString("N0")+" 已到账；下次日结开始自动还款。";
            Trace("loan.taken",reason,loan);return true;
        }
        public bool RepayLoanEarly(int id,out string reason)
        {
            var loan=loans.contracts.FirstOrDefault(l=>l.id==id);
            if(loan==null || LoanRemaining(loan)==0){reason="贷款不存在或已还清。";return false;}
            int remaining=LoanRemaining(loan);
            if(money<remaining){reason="资金不足；还清需要 ¥"+remaining.ToString("N0")+"。";return false;}
            money-=remaining;loan.paid+=remaining;
            reason="已还清贷款，支付 ¥"+remaining.ToString("N0")+"，不再自动扣款。";
            Trace("loan.repaid_early",reason,loan);return true;
        }
        public int SettleLoans()
        {
            if(!development.enabled || loans.lastSettledDay==day)return 0;
            int payment=0;
            foreach(var loan in loans.contracts)
            {int due=Math.Max(0,ScheduledPaid(loan,day)-loan.paid);if(due==0)continue;
                money=checked(money-due);loan.paid+=due;payment+=due;
                Trace("loan.installment","按合同自动偿还 ¥"+due+(LoanRemaining(loan)==0?"；贷款已结清":""),loan);}
            loans.lastSettledDay=day;return payment;
        }
        bool ValidLoans()
        {
            if(loans==null || loans.contracts==null || loans.contracts.Count>10000 || loans.nextId<1 || loans.nextId==int.MaxValue || loans.lastSettledDay<0 || loans.lastSettledDay>day)return false;
            var ids=new HashSet<int>();var active=new HashSet<int>();
            foreach(var loan in loans.contracts)
            {
                if(loan==null || loan.id<=0 || loan.id>=loans.nextId || !ids.Add(loan.id) || LoanOffer(loan.offerId)==null || loan.takenDay<1 || loan.takenDay>day || loan.paid<0 || loan.paid>LoanOffer(loan.offerId).Total)return false;
                if(LoanRemaining(loan)>0 && !active.Add(loan.offerId))return false;
                if(loan.paid<ScheduledPaid(loan,loans.lastSettledDay))return false;
            }
            return true;
        }
    }
}
