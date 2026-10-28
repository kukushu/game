import copy
import unittest

from landlab.session import LandSession


class PresentationFactsTests(unittest.TestCase):
    def test_readonly_views_and_road_preview_do_not_change_the_simulation(self):
        s=LandSession.new(1)
        before=copy.deepcopy(s.dump())
        view=s.view()
        road=s.inspect('road',9*35+16)
        self.assertEqual(road['cost'],1200)
        self.assertGreater(road['duration_before'],road['duration_with_connection'])
        self.assertEqual(before,s.dump())
        self.assertEqual(len(view['worlds']['player']['producers']),4)
        self.assertEqual(sum(n['housing_land'] for n in view['worlds']['player']['nodes']),
                         sum((sum(h.node==n for h in s.player.residents)+3)//4 for n in range(35)))

    def test_feedback_uses_actual_commute_records_and_same_households(self):
        s=LandSession.new(1)
        s.intervene('road',{'id':9,'neighbor':16})
        s.advance(3)
        view=s.view();feedback=view['player_feedback']
        self.assertEqual(feedback['periods'],3)
        expected_less=expected_more=0
        for h in s.player.residents:
            values=[]
            for c in (s.player,s.reference):
                values.append(sum(r['duration'] for t in c.traces[-3:] for r in t.get('commute_trip',[]) if r['resident']==h.id)/3)
            expected_less+=values[0]<values[1]-1e-6
            expected_more+=values[0]>values[1]+1e-6
        self.assertEqual(feedback['commute_less'],expected_less)
        self.assertEqual(feedback['commute_more'],expected_more)
        users={r['resident'] for t in s.player.traces[-3:] for r in t.get('commute_trip',[])
               if [9,16] in r['edges']}
        self.assertEqual(feedback['new_road_users'],len(users))
        for e in feedback['examples']:
            self.assertEqual(e['node'],s.player.residents[e['id']].node)
            self.assertEqual(e['reference_node'],s.reference.residents[e['id']].node)

    def test_empty_commercial_permission_does_not_create_a_presented_shop(self):
        s=LandSession.new(1)
        s.intervene('zone',{'id':11,'zone':'shop'})
        view=s.view()['worlds']['player']
        self.assertEqual(view['nodes'][11]['zone'],'shop')
        self.assertEqual(view['nodes'][11]['stores'],[])
        self.assertFalse(any(x['active'] and x['node']==11 for x in view['stores']))


if __name__=='__main__':unittest.main()
