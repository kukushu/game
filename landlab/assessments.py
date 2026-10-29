"""Persist actual commercial site visits without changing investment decisions."""
import copy
import math


class CommercialAudit:
    def __init__(self, full_since=0):
        self.full_since = full_since
        self.visits = []
        self.rounds = []
        self.active = False
        self._current = {}

    def begin(self, city):
        count = sum(s.active for s in city.stores)
        reason = ('entry_disabled' if not city.settings.entry else
                  'direct_only' if city.settings.channel == 'direct' else
                  'store_limit' if count >= 18 else None)
        self._round = {'month': city.month, 'fund_cash': city.accounts['fund'],
                       'active_stores': count, 'status': reason or 'assessing', 'winner': None}
        self.rounds.append(self._round)
        self._event_start = len(city.events)
        self._current = {}
        self.active = reason is None

    def schedule(self, eligible, selected):
        if self.active:
            self._round.update({'selection_rule': 'eligible_least_recent',
                                'eligible_nodes': list(eligible), 'selected_nodes': list(selected)})

    def visit(self, city, node, areas):
        if not self.active:
            return
        from .model import RIGHTS
        connected = city.location_accessible(node)
        permitted = 'C' in RIGHTS[city.zones[node]]
        free = city.free_land(node)
        blockers = []
        if not permitted:
            blockers.append('no_commercial_permission')
        if not connected:
            blockers.append('disconnected')
        if free < 1:
            blockers.append('insufficient_land')
        if not areas and not blockers:
            blockers.append('no_legal_size')
        record = {'id': f'{city.month}:{node}', 'month': city.month, 'node': node,
                  'zone': city.zones[node], 'free_land': free, 'connected': connected,
                  'allowed_areas': list(areas), 'fund_cash': city.accounts['fund'],
                  'households': len(city.residents),
                  'reachable_households': sum(2*city.route(h.node, node)[0]+.06 <= city.settings.shopping_hours
                                               for h in city.residents),
                  'blockers': blockers, 'legacy_partial': False, 'plans': []}
        self.visits.append(record)
        self._current[node] = record

    def estimate(self, city, details):
        if not self.active or details['node'] not in self._current:
            return
        visit = self._current[details['node']]
        node, area = details['node'], details['area']
        rent = city.rent(node, area)
        hours = city.staff_hours(area)
        wages = round(hours*100)
        working = round(48*area*130 + 2*(rent+hours*100))
        fee = 800+600*area
        revenue = purchases = freight = 0.
        products = []
        for good, quantity in details['demand'].items():
            base = min(p.price for p in city.producers if p.good == good)
            supplier = min((p for p in city.producers if p.good == good),
                           key=lambda p: city.route(p.node, node, 'truck')[0])
            cost, labor, trips = city.delivery_cost(supplier.node, node, math.ceil(quantity)) if quantity >= 1 else (0, 0., 0)
            revenue += details['prices'][good]*quantity
            purchases += base*quantity
            freight += cost
            products.append({'good': good, 'quantity': quantity, 'price': details['prices'][good],
                             'base_price': base, 'supplier': supplier.id, 'source': supplier.node,
                             'supplier_stock': supplier.stock, 'freight': cost,
                             'freight_hours': labor, 'trips': trips})
        plan = copy.deepcopy(details)
        plan.update({'working_capital': working, 'setup_fee': fee,
                     'score': details['profit']/details['capital'],
                     'profitable': details['profit'] > 0,
                     'costs': {'revenue': revenue, 'purchases': purchases, 'freight': freight,
                               'rent': rent, 'staff_hours': hours, 'staff_wages': wages,
                               'operating_fee': 100, 'total': purchases+freight+rent+wages+100},
                     'products': products, 'decision': 'pending'})
        visit['plans'].append(plan)

    def finish(self, city):
        entries = [e for e in city.events[self._event_start:] if e['event'] == 'entry']
        winner = None
        if entries:
            event = entries[-1]
            store = city.stores[event['store']]
            winner = {'store': store.id, 'node': store.node, 'area': store.area,
                      'assortment': list(store.assortment),
                      'score': event['forecast_profit']/(event['capital']+event['fee'])}
            self._round['winner'] = winner
        if self.active:
            self._round['status'] = ('no_eligible_sites' if self._round.get('eligible_nodes') == [] else
                                     'invested' if winner else 'no_investment')
        for visit in self._current.values():
            visit['winner'] = copy.deepcopy(winner)
            for plan in visit['plans']:
                selected = (winner is not None and winner['node'] == visit['node'] and
                            winner['area'] == plan['area'] and winner['assortment'] == plan['assortment'])
                blockers = []
                if plan['profit'] <= 0:
                    blockers.append('non_positive_profit')
                if not plan['affordable']:
                    blockers.append('insufficient_capital')
                plan['blockers'] = blockers
                plan['decision'] = ('selected' if selected else 'rejected' if blockers else 'other_site_selected')
                if selected:
                    plan['store'] = winner['store']
        # Keep the whole contemporary comparison once per round, rather than
        # reconstructing competitors from today's prices or storing only a winner.
        proposals = [{'node': v['node'], **{k: copy.deepcopy(p[k]) for k in
                      ('area','assortment','profit','capital','affordable','score','decision','blockers')}}
                     for v in self._current.values() for p in v['plans']]
        qualified = sorted((p for p in proposals if not p['blockers']),key=lambda p:-p['score'])
        for p in proposals:
            p['rank'] = next((i+1 for i,q in enumerate(qualified) if q is p),None)
        self._round['proposals'] = proposals
        self._round['comparison_rule'] = 'positive_profit_affordable_max_return_first_tie'
        self.active = False

    def dump(self):
        return copy.deepcopy({'version': 1, 'full_since': self.full_since,
                              'visits': self.visits, 'rounds': self.rounds})

    @classmethod
    def restore(cls, data, city):
        result = cls(city.month)
        if data is not None:
            if data.get('version') != 1 or not isinstance(data.get('visits'), list) or not isinstance(data.get('rounds'), list):
                raise ValueError('商业考察历史无效')
            result.full_since = data['full_since']
            result.visits = copy.deepcopy(data['visits'])
            result.rounds = copy.deepcopy(data['rounds'])
            return result
        # Preserve only numbers that really survived in an older save. Never reconstruct
        # historical costs, permissions or cash from present-day conditions.
        for trace in city.traces:
            grouped = {}
            for estimate in trace.get('site_estimate', []):
                node = estimate['node']
                if node not in grouped:
                    grouped[node] = {'id': f"{trace['month']}:{node}", 'month': trace['month'],
                                     'node': node, 'legacy_partial': True, 'blockers': [], 'plans': []}
                plan = copy.deepcopy(estimate)
                plan['decision'] = 'legacy_unknown'
                grouped[node]['plans'].append(plan)
            result.visits.extend(grouped.values())
        return result

    def for_node(self, node):
        records = [copy.deepcopy(v) for v in self.visits if v['node'] == node]
        rounds = {r['month']: r for r in self.rounds}
        for record in records:
            record['comparison'] = copy.deepcopy(rounds.get(record['month']))
        return {'full_since': self.full_since, 'visits': records,
                'total_visits': len(records), 'total_plans': sum(len(v['plans']) for v in records),
                'last_round': copy.deepcopy(self.rounds[-1]) if self.rounds else None}

    def for_store(self, identity):
        for round_ in self.rounds:
            if (round_.get('winner') or {}).get('store') == identity:
                result = copy.deepcopy(round_)
                result['selected_plan'] = next((copy.deepcopy(p) for v in self.visits
                    if v['month'] == round_['month'] for p in v['plans'] if p.get('store') == identity),None)
                return result
        return None
