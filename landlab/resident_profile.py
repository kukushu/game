"""Read-only household facts: observed payments versus present-day quotations."""
import math

WAGES = frozenset(('manufacturing', 'factory_service', 'retail_staff', 'freight'))
DIVIDENDS = frozenset(('household_dividend', 'fund_dividend'))


def resident_profile(city, resident, payments):
    account = f'h{resident.id}'
    incoming = sum(p['cents'] for p in payments if p['target'] == account)
    outgoing = sum(p['cents'] for p in payments if p['source'] == account)
    def received(purposes):
        return sum(p['cents'] for p in payments if p['target'] == account and p['purpose'] in purposes)
    def spent(purposes):
        return sum(p['cents'] for p in payments if p['source'] == account and p['purpose'] in purposes)
    wages, dividends = received(WAGES), received(DIVIDENDS)
    tax = spent({'income_tax'})
    opening = city.accounts[account]-incoming+outgoing
    balance, shopping_cash = opening, None
    for p in payments:
        if p['source'] == account and p['purpose'] in ('direct_sale','retail_sale') and shopping_cash is None:
            shopping_cash = balance
        balance += p['cents'] if p['target'] == account else -p['cents']
    if shopping_cash is None:
        blocked = next((r for r in city.current.get('shopping_blocked',[]) if r['resident']==resident.id),None)
        if blocked:shopping_cash = blocked['cash']
    jobs = {}
    for p in payments:
        if p['target'] == account and p['purpose'] in WAGES:
            key = p['source'], p['purpose']
            jobs[key] = jobs.get(key, 0) + p['cents']
    offers = []
    for seller, node, prices in city.offers():
        trip = 2*city.route(resident.node, node)[0]+.06
        for good, price in prices.items():
            offers.append({'seller':seller, 'node':node, 'good':good, 'price':price,
                           'stock':city.seller_stock(seller,good), 'service':city.seller_service(seller),
                           'roundtrip':trip if math.isfinite(trip) else None,
                           'within_time_limit':trip <= city.settings.shopping_hours})
    last_move = city.moved_home.get(resident.id)
    earliest = max(6, (last_move+4) if last_move is not None else 6)
    return {
        'period':city.month, 'has_settled':city.month > 0,
        'cash':city.accounts[account],
        'cash_open':opening, 'cash_before_shopping':shopping_cash,
        'income':{'wages':wages, 'dividends':dividends, 'gross':wages+dividends,
                  'tax':tax, 'net':wages+dividends-tax,
                  'observed_tax_fraction':tax/(wages+dividends) if wages+dividends else None},
        'payments':{'incoming':incoming, 'outgoing':outgoing, 'change':incoming-outgoing,
                    'shopping':spent({'direct_sale','retail_sale'}),
                    'housing':spent({'housing_rent'}), 'moving':spent({'home_move'})},
        'housing':{'quote':city.home_rent(resident.node), 'exposure':city.exposure(resident.node),
                   'environment_preference':city.preferences[resident.id],
                   'environment_weight':city.environment_weight},
        'time':{'value':resident.value_time, 'wage_per_hour':100,
                'work':resident.work, 'commute':city.commute.get(resident.id,0),
                'shopping':resident.shopping, 'work_commute_limit':8,
                'shopping_limit':city.settings.shopping_hours},
        'jobs':[{'seller':seller, 'node':city.employer_node(seller), 'purpose':purpose,
                 'hours':amount/100, 'gross_wage':amount} for (seller,purpose),amount in jobs.items()],
        'posted_offers':offers,
        'move':{'last_month':last_move+1 if last_move is not None else None,
                'earliest_month':earliest+1, 'cooldown':4, 'fee':120,
                'policy':city.home_move_policy, 'mode':city.mode},
        'current_tax_rate':city.income_tax_rate,
    }
