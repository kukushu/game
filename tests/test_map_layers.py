import copy
import unittest

from landlab.session import LandSession
from landlab.layers import city_layers, WAGE_PURPOSES


class MapLayerTests(unittest.TestCase):
    def test_read_only_population_quotes_and_separate_reference(self):
        session = LandSession.new(1)
        before = copy.deepcopy(session.dump())
        view = session.view()
        self.assertEqual(before, session.dump())
        nodes = view['worlds']['player']['nodes']
        self.assertEqual(sum(n['layer_data']['population'] for n in nodes), 64)
        node = next(n for n in nodes if n['zone'] == 'home')
        self.assertEqual(node['layer_data']['housing_rent'], session.player.home_rent(node['id']) / 100)
        self.assertIsNone(next(n for n in nodes if n['zone'] == 'factory')['layer_data']['housing_rent'])
        session.intervene('zone', {'id': 11, 'zone': 'home'})
        view = session.view()
        self.assertIsNotNone(view['worlds']['player']['nodes'][11]['layer_data']['housing_rent'])
        self.assertIsNone(view['worlds']['reference']['nodes'][11]['layer_data']['housing_rent'])
        occupied = next(h.node for h in session.player.residents if session.player.zones[h.node] == 'home')
        session.intervene('zone', {'id': occupied, 'zone': 'shop'})
        self.assertIsNotNone(city_layers(session.player)[occupied]['housing_rent'],
                             'Existing residents retain a rent quote after rezoning')

    def test_wage_and_net_income_reconcile_with_real_payments_after_tax(self):
        session = LandSession.new(2)
        session.intervene('tax', {'rate': 10})
        session.advance(2)
        city = session.player
        layers = city_layers(city)
        total_wages = sum(p['cents'] for p in city.current['payments'] if p['purpose'] in WAGE_PURPOSES)
        reconstructed = sum(x['wage_income'] * x['population'] * 100
                            for x in layers.values() if x['wage_income'] is not None)
        self.assertAlmostEqual(total_wages, reconstructed)
        net = sum(x['net_income'] * x['population'] * 100
                  for x in layers.values() if x['net_income'] is not None)
        self.assertAlmostEqual(net, city.fiscal_summary()['net_income'])
        self.assertTrue(all(x['wage_income'] is None or 0 <= x['wage_income'] <= 8 for x in layers.values()))
        self.assertTrue(all(x['population'] or x['wage_income'] is None for x in layers.values()))

    def test_blank_and_unsettled_are_not_fake_zero_incomes_or_land_values(self):
        plan = LandSession.new(1, 'blank')
        for node in plan.view()['worlds']['player']['nodes']:
            self.assertEqual(node['layer_data']['population'], 0)
            self.assertTrue(all(v is None for k, v in node['layer_data'].items() if k != 'population'))
        for a, b in ((8, 9), (9, 10), (10, 11)):
            plan.intervene('road', {'id': a, 'neighbor': b})
        for node, zone in ((8, 'home'), (9, 'home'), (10, 'factory'), (11, 'factory')):
            plan.intervene('zone', {'id': node, 'zone': zone})
        session = plan.launch()
        for node in session.view()['worlds']['player']['nodes']:
            self.assertIsNone(node['layer_data']['wage_income'])
            self.assertIsNone(node['layer_data']['shortage'])
        session.advance(1)
        layers = city_layers(session.player)
        shortage = sum(x['shortage'] * x['population'] for x in layers.values() if x['shortage'] is not None)
        self.assertAlmostEqual(shortage, sum(4 - h.food - h.goods for h in session.player.residents))


if __name__ == '__main__':
    unittest.main()
