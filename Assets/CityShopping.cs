using System;
using System.Linq;

namespace HarborCity
{
    public sealed partial class CityTraffic
    {
        public TrafficTrip DispatchShopping(int home,int shop,int citizen)
        {
            var family=city.society.families.FirstOrDefault(h=>h.resident && h.home==home && h.people.Any(p=>p.id==citizen));
            var person=family?.people.Find(p=>p.id==citizen);var store=city.GetBuilding(shop) as CommercialBuilding;
            if(!city.development.enabled || person==null || person.medicalStage!=MedicalStage.None || person.sick || person.age<18 || person.tripId!=0 || person.atWork || person.location!=home
                || store==null || store.abandoned || store.stock==0 || !city.HasBasicServices(shop) || city.EmployedAt(shop)==0
                || State.trips.Count>=TaskCapacity || State.nextId==int.MaxValue)return null;
            var route=FindRoute(home,shop);if(route.Count==0 || !CanEnter(route,null))return null;
            var trip=new TrafficTrip{id=State.nextId++,origin=home,destination=shop,home=home,residentId=citizen,householdId=family.id,
                purpose=TripPurpose.Shopping,route=route,departedAt=State.clock};
            State.trips.Add(trip);person.tripId=trip.id;family.shoppingDay=city.day;
            city.Trace("trip.started","家庭成员实际出发购物",trip,household:family.id,citizen:citizen,trip:trip.id);return trip;
        }
        void ScheduleShopping()
        {
            if(!city.development.enabled || city.ResidentMinute<1020)return;
            // Frequency and time use the current prototype clock; original scheduling
            // is unverified. No new private-budget/price-choice system is introduced.
            foreach(var h in city.society.families.Where(h=>h.resident && h.shoppingDay!=city.day))
            {
                var person=h.people.FirstOrDefault(p=>!p.sick && p.age>=18 && !p.atWork && p.tripId==0 && p.location==h.home && State.clock>=p.retryAt);
                if(person==null)continue;person.retryAt=State.clock+.5f;
                foreach(var shop in city.buildings.OfType<CommercialBuilding>().Where(b=>!b.abandoned && b.stock>0 && city.HasBasicServices(b.id))
                    .OrderBy(b=>city.CommuteMinutes(h.home,b.id)).ThenBy(b=>b.id))
                    if(DispatchShopping(h.home,shop.id,person.id)!=null)break;
            }
        }
        void ArriveShopping(TrafficTrip trip)
        {
            var family=TripFamily(trip);var person=family?.people.Find(p=>p.id==trip.residentId);
            if(family==null || person==null){State.lostGoods+=trip.cargo;State.trips.Remove(trip);return;}
            person.location=trip.destination;person.accessNode=trip.Current;person.atWork=false;
            if(trip.returning)
            {
                State.consumedGoods+=trip.cargo;trip.cargo=0;person.tripId=0;State.completed++;State.trips.Remove(trip);
                city.Trace("shopping.home","居民将已购商品实际带回家消费",trip,household:family.id,citizen:person.id,trip:trip.id);return;
            }
            var shop=city.GetBuilding(trip.destination) as CommercialBuilding;
            if(shop!=null)
            {
                shop.customers++;if(shop.retailDay!=city.day){shop.retailDay=city.day;shop.retailSoldToday=0;}
                if(shop.stock>0 && city.HasBasicServices(shop.id) && city.EmployedAt(shop.id)>0)
                {shop.stock--;shop.retailSold++;shop.retailSoldToday++;trip.cargo=1;}
            }
            city.Trace("shopping.arrived",trip.cargo>0?"居民实际到店购买一件库存商品；私人付款暂简化":"实际到店但无可购买商品",trip,household:family.id,citizen:person.id,building:trip.destination,trip:trip.id);
            trip.status=TripStatus.Visiting;trip.delay=2;trip.blocked=0;
        }
    }
}
