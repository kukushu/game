import copy
import unittest

from landlab.session import LandSession


class ResidentProfileTests(unittest.TestCase):
    def test_post_shopping_dividend_is_not_reported_as_shopping_budget(self):
        session = LandSession.new(3)
        city = session.player
        city.current = {'month':city.month}
        opening = city.accounts['h0']
        city.pay('h0','p0',100,'direct_sale')
        city.pay('fund','h0',200,'fund_dividend')
        profile = session.inspect('resident',0)['profile']
        self.assertEqual(profile['cash_open'],opening)
        self.assertEqual(profile['cash_before_shopping'],opening)
        self.assertEqual(profile['cash'],opening+100)

    def test_profile_reconciles_real_cash_and_work_without_mutation(self):
        session = LandSession.new(7)
        session.intervene('tax', {'rate':30})
        session.advance(1)
        before = copy.deepcopy(session.dump())
        for h in session.player.residents:
            d = session.inspect('resident',h.id)
            p = d['profile']; flow = p['payments']; income = p['income']
            self.assertEqual(p['cash'],d['cash'])
            self.assertEqual(p['cash_open']+flow['incoming']-flow['outgoing'],p['cash'])
            self.assertEqual(income['gross']-income['tax'],income['net'])
            self.assertEqual(income['gross'],d['income']['gross'])
            self.assertEqual(sum(j['gross_wage'] for j in p['jobs']),income['wages'])
            self.assertAlmostEqual(sum(j['hours'] for j in p['jobs']),h.work)
            self.assertEqual(flow['shopping'],sum(x['cents'] for x in d['purchases']))
            self.assertEqual(p['housing']['quote'],session.player.home_rent(h.node))
        self.assertEqual(before,session.dump())

    def test_transaction_prices_are_not_replaced_by_current_quotes(self):
        session = LandSession.new(2)
        buyer = next(h for h in session.player.residents if session.inspect('resident',h.id)['purchases'])
        before = copy.deepcopy(session.inspect('resident',buyer.id)['purchases'])
        for p in session.player.producers: p.price += 99
        for s in session.player.stores:
            for g in s.prices:s.prices[g] += 99
        d = session.inspect('resident',buyer.id)
        self.assertEqual(before,d['purchases'])
        for receipt in d['purchases']:
            quote = next(o for o in d['profile']['posted_offers']
                         if o['seller']==receipt['seller'] and o['good']==receipt['good'])
            self.assertEqual(quote['price'],receipt['price_cents']+99)
            self.assertEqual(receipt['price_cents']*receipt['quantity'],receipt['cents'])

    def test_current_policy_is_distinct_from_observed_tax_and_reference(self):
        session = LandSession.new(3)
        before = copy.deepcopy(session.inspect('resident',0)['profile'])
        session.intervene('tax', {'rate':30})
        profile = session.inspect('resident',0)['profile']
        self.assertEqual(profile['current_tax_rate'],30)
        self.assertEqual(profile['income'],before['income'])
        self.assertEqual(session.inspect('resident',0,'reference')['profile']['current_tax_rate'],0)

    def test_new_city_has_no_fabricated_receipts_or_work(self):
        from test_blank_start import prepared
        session = prepared().launch()
        d = session.inspect('resident',0)
        self.assertFalse(d['profile']['has_settled'])
        self.assertEqual(d['purchases'],[])
        self.assertEqual(d['profile']['jobs'],[])
        self.assertEqual(d['profile']['income']['gross'],0)
        self.assertEqual(d['profile']['cash_open'],d['cash'])


if __name__ == '__main__':unittest.main()
