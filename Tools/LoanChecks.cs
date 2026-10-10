using System;
using System.Linq;
using HarborCity;

public static class LoanChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Loans: "+why);checks++;}
    public static void Run()
    {
        var c=CityModel.Create();int money=c.money,income=c.income;
        Check(c.TakeLoan(0,out _) && c.money==money+20000 && c.LoanDebt==21000 && c.income==income && c.Valid(),"Principal is cash, not tax income");
        money=c.money;Check(!c.TakeLoan(0,out _) && !c.TakeLoan(-1,out _) && c.money==money,"Duplicate or unknown offer cannot credit cash");
        Check(c.TakeLoan(1,out _) && c.TakeLoan(2,out _) && c.LoanDebt==317000,"Three distinct contracts coexist");
        c=CityModel.Create();c.TakeLoan(0,out _);c.Tick();var d=c.society.history.Last();
        Check(d.loanPayment==57 && d.closingTreasury==d.openingTreasury+d.tax-d.maintenance-57 && c.LoanDebt==20943 && c.LoanPaymentNextDay==58 && c.Valid(),"Daily ledger and fractional installment rounding");
        money=c.money;Check(c.SettleLoans()==0 && c.money==money,"Same-day settlement cannot charge twice");
        var saved=TestCity.RoundTrip(c);c.Tick();saved.Tick();Check(saved.money==c.money && saved.LoanDebt==c.LoanDebt && saved.Valid(),"Saved contract continues identically");
        c.money=0;int debt=c.LoanDebt;Check(!c.RepayLoanEarly(c.loans.contracts[0].id,out _) && c.money==0 && c.LoanDebt==debt,"Insufficient cash preserves debt");
        c.money=debt+123;Check(c.RepayLoanEarly(c.loans.contracts[0].id,out _) && c.money==123 && c.LoanDebt==0 && c.LoanPaymentNextDay==0 && c.Valid(),"Early repayment pays displayed outstanding total");
        int previous=c.loans.contracts[0].id;Check(c.TakeLoan(0,out _) && c.ActiveLoan(0).id>previous,"Repaid slot reusable with new stable ID");
        foreach(var offer in CityModel.LoanOffers)
        {
            c=CityModel.Create();c.TakeLoan(offer.id,out _);int total=0;
            for(int n=0;n<offer.Days;n++){c.Tick();total+=c.LastLoanPayment;}
            Check(total==offer.Total && c.LoanDebt==0 && c.ActiveLoan(offer.id)==null && c.Valid(),"Exact full-term total, offer "+offer.id);
            c.Tick();money=c.money;Check(c.LastLoanPayment==0 && c.SettleLoans()==0 && c.money==money,"No charge after maturity, offer "+offer.id);
        }
        c=CityModel.Create();c.TakeLoan(0,out _);c.money=-100;c.Tick();Check(c.money<-100 && c.loans.contracts[0].paid==57 && c.Valid(),"Negative treasury still records repayment");
        c=CityModel.Create();var dto=c.ToSaveData();dto.format=23;dto.loans=null;money=c.money;var migrated=dto.ToCity();
        Check(migrated.money==money && migrated.LoanDebt==0 && migrated.Valid() && migrated.ToSaveData().format==24,"Format 23 migrates without changing money");
        dto=migrated.ToSaveData();dto.loans=null;bool rejected=false;try{dto.ToCity();}catch(ArgumentException){rejected=true;}Check(rejected,"Format 24 requires loan state");
        c=CityModel.Create();c.TakeLoan(0,out _);saved=TestCity.RoundTrip(c);saved.loans.contracts[0].paid=21001;Check(!saved.Valid(),"Overpaid contract rejected");
        saved=TestCity.RoundTrip(c);saved.loans.contracts[0].offerId=9;Check(!saved.Valid(),"Unknown saved offer rejected");
        saved=TestCity.RoundTrip(c);saved.loans.contracts.Add(new CityLoan{id=saved.loans.nextId++,offerId=0,takenDay=saved.day});Check(!saved.Valid(),"Duplicate active slot rejected");
        saved=TestCity.RoundTrip(c);saved.day++;saved.loans.lastSettledDay=saved.day;Check(!saved.Valid(),"Missing payment cannot claim settled day");
        c=TestCity.Empty();money=c.money;Check(!c.TakeLoan(0,out _) && c.money==money,"Prototype mode cannot borrow");
        c=CityModel.Create();c.money=int.MaxValue;Check(!c.TakeLoan(0,out _) && c.loans.contracts.Count==0,"Overflow cannot partially create contract");
        c=CityModel.Create();c.loans.nextId=int.MaxValue-1;Check(!c.TakeLoan(0,out _) && c.Valid(),"ID exhaustion cannot create an invalid contract");
        Console.WriteLine("PASS: "+checks+" loan cash/ledger/repayment/save/migration checks");
    }
}
