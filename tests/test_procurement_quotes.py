import copy
import unittest
from unittest.mock import patch

from commerce.model import City, Settings, Store
from landlab.model import LandCity


class ProcurementQuoteTests(unittest.TestCase):
    def setUp(self):
        self.city=City(7,Settings(households=24,entry=False))
        self.a,self.b=self.city.producers[0],self.city.producers[2]
        self.a.price,self.b.price=100,120

    def freight(self,source,target,quantity):
        return (40 if source==self.a.node else 10),0.,1

    def test_example_never_combines_two_factories_into_fictitious_110(self):
        with patch.object(self.city,'delivery_cost',side_effect=self.freight):
            quote=self.city.procurement_quotes('food',0,1)[0]
            forecast=self.city.forecast_procurement(0,{'food':1},{'food':200})
        self.assertEqual(quote['supplier'],self.b.id)
        self.assertEqual(quote['landed_cost'],130)
        self.assertEqual(forecast['purchases']+forecast['freight'],130)
        self.assertNotEqual(forecast['purchases']+forecast['freight'],110)

    def test_batch_size_can_reverse_supplier_ranking(self):
        with patch.object(self.city,'delivery_cost',side_effect=self.freight):
            self.assertEqual(self.city.procurement_quotes('food',0,1)[0]['supplier'],self.b.id)
            self.assertEqual(self.city.procurement_quotes('food',0,10)[0]['supplier'],self.a.id)

    def test_quote_is_not_a_stock_reservation_and_subunit_demand_pays_freight(self):
        self.a.stock=self.b.stock=0
        before=copy.deepcopy(self.city.snapshot())
        forecast=self.city.forecast_procurement(0,{'food':.5},{'food':200})
        product=forecast['products'][0]
        self.assertEqual(product['batch_quantity'],1)
        self.assertGreater(product['freight'],0)
        self.assertFalse(product['supply_guaranteed'])
        self.assertEqual(self.city.snapshot(),before)

    def test_actual_retail_orders_use_the_same_joint_quote_order(self):
        c=self.city
        self.a.stock=self.b.stock=10
        s=Store(0,0,1,['food'],{'food':200},{'food':0},{'food':1},{'food':130})
        c.stores=[s];c.accounts['r0']=0;c.pay('fund','r0',10000,'equity_investment')
        with patch.object(c,'delivery_cost',side_effect=self.freight):
            expected=c.procurement_quotes('food',0,2)[0]['supplier']
            c.operate_stores()
        self.assertEqual(expected,self.a.id)
        self.assertEqual(self.a.stock,8)
        self.assertEqual(self.b.stock,10)
        c.check()

    def test_recorded_breakdown_uses_the_same_supplier_as_estimate(self):
        c=LandCity(7);c.zones=dict.fromkeys(range(35),'open');c.step()
        for v in c.commercial_audit.visits:
            for p in v['plans']:
                costs=p['costs']
                self.assertAlmostEqual(costs['revenue']-costs['total'],p['profit'])
                self.assertAlmostEqual(costs['purchases'],sum(q['expected_goods_cost'] for q in p['products']))
                self.assertEqual(costs['freight'],sum(q['freight'] for q in p['products']))
                for q in p['products']:
                    self.assertEqual(q['quote_rule'],'joint_price_freight')
                    self.assertEqual(q['base_price'],c.producers[q['supplier']].price)


if __name__=='__main__':unittest.main()
