import copy
import json
import unittest
from dataclasses import asdict

from commerce.model import City, Settings, Store


class CommerceTests(unittest.TestCase):
    def city(self, **kwargs):
        return City(7, Settings(households=24, **kwargs), trace=True)

    def test_channel_initial_endowments_and_residents_are_identical(self):
        cities=[self.city(channel=mode) for mode in ('direct','retail','mixed')]
        self.assertEqual(cities[0].accounts,cities[1].accounts)
        self.assertEqual(cities[0].accounts,cities[2].accounts)
        self.assertEqual([asdict(h) for h in cities[0].residents],[asdict(h) for h in cities[2].residents])

    def test_cash_and_physical_goods_conserved_all_channels(self):
        for mode in ('direct','retail','mixed'):
            for seed in (1,7):
                city=City(seed,Settings(households=24,channel=mode))
                for _ in range(72):
                    row=city.step()
                    self.assertEqual(row['cash_error'],0)
                    self.assertEqual(row['stock_error_food'],0)
                    self.assertEqual(row['stock_error_goods'],0)

    def test_reconstruct_full_ledger_and_never_overdraw(self):
        city=self.city()
        accounts=city.accounts.copy()
        for _ in range(32):
            city.step()
        for t in city.ledger:
            accounts.setdefault(t['source'],0)
            accounts.setdefault(t['target'],0)
            accounts[t['source']]-=t['cents']
            accounts[t['target']]+=t['cents']
            self.assertGreaterEqual(accounts[t['source']],0)
        self.assertEqual(accounts,city.accounts)

    def test_snapshot_exact_resume_and_no_aliasing(self):
        city=self.city()
        for _ in range(8):
            city.step()
        snapshot=json.loads(json.dumps(city.snapshot()))
        frozen=copy.deepcopy(snapshot)
        branch=City.restore(snapshot,trace=True)
        for _ in range(16):
            city.step()
            branch.step()
        self.assertEqual(city.snapshot(),branch.snapshot())
        self.assertEqual(snapshot,frozen)

    def test_bulk_transport_uses_trucks_not_discounted_goods(self):
        city=self.city()
        source,target=city.node(6,2),city.node(1,2)
        price_single,_,trips_single=city.delivery_cost(source,target,1)
        price_full,_,trips_full=city.delivery_cost(source,target,32)
        self.assertEqual(trips_single,1)
        self.assertEqual(trips_full,1)
        self.assertLess(price_full/32,price_single)
        self.assertEqual(city.producers[0].price,100)

    def test_shopping_tour_can_share_distance_without_attraction_bonus(self):
        city=self.city()
        home=city.node(0,2)
        nodes=[city.node(2,2),city.node(3,2)]
        chained=city.itinerary(home,nodes)[0]
        city.settings.chain_trips=False
        separate=city.itinerary(home,nodes)[0]
        self.assertLess(chained,separate)

    def test_road_upgrade_shortens_actual_freight_path(self):
        a=self.city()
        b=self.city(corridor=True)
        self.assertLess(b.route(b.node(0,2),b.node(6,2),'truck')[0],a.route(a.node(0,2),a.node(6,2),'truck')[0])
        self.assertEqual(b.route(b.node(0,2),b.node(6,2))[0],a.route(a.node(0,2),a.node(6,2))[0])

    def test_land_and_staff_capacity_are_constraints(self):
        city=self.city(zoning='small_only')
        for _ in range(24):
            city.step()
        self.assertTrue(all(s.area==1 for s in city.stores))
        self.assertTrue(all(sum(s.stock.values())<=s.capacity for s in city.stores))
        self.assertEqual(len({s.node for s in city.stores if s.active}),sum(s.active for s in city.stores))

    def test_no_capital_no_free_shops(self):
        city=self.city(channel='retail',initial_capital=0)
        for _ in range(8):
            city.step()
        self.assertEqual(len(city.stores),0)
        self.assertEqual(city.metrics[-1]['units'],0)

    def test_short_shopping_budget_cannot_be_ignored(self):
        city=self.city(channel='direct',shopping_hours=.001)
        for _ in range(3):
            city.step()
        self.assertEqual(city.metrics[-1]['units'],0)

    def test_no_labor_no_production(self):
        city=self.city()
        for h in city.residents:
            h.work=8
        city.production()
        self.assertEqual(sum(p.stock for p in city.producers),0)

    def test_stock_writeoff_is_not_hidden_on_store_exit(self):
        city=self.city()
        s=Store(0,0,1,['food'],{'food':150},{'food':10},{'food':10},{'food':100})
        city.stores.append(s)
        city.accounts['r0']=0
        city.close(s,'test')
        self.assertEqual(city.wasted['food'],10)
        self.assertEqual(city.operating_profit,-1000)
        self.assertFalse(s.active)

    def test_invalid_settings(self):
        for settings in (Settings(channel='unknown'),Settings(truck_load=0),Settings(customer_edge_hours=-1),Settings(households=1)):
            with self.assertRaises(ValueError):
                City(settings=settings)

    def test_retail_only_settings_do_not_change_direct_economy(self):
        a=self.city(channel='direct')
        b=self.city(channel='direct',corridor=True,small_multigood=True,freight_multiplier=6,zoning='small_only')
        for _ in range(16):
            self.assertEqual(a.step(),b.step())
        self.assertEqual(a.accounts,b.accounts)


if __name__=='__main__':
    unittest.main()
