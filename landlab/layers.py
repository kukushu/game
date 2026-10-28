"""Read-only map aggregates. No agent decisions, payments, or inferred land prices."""
from .model import RIGHTS, TAXABLE_INCOME

WAGE_PURPOSES = TAXABLE_INCOME - {'household_dividend', 'fund_dividend'}
LAYER_KEYS = ('population', 'housing_rent', 'wage_income', 'net_income',
              'commute', 'shortage', 'exposure')


def planning_layers():
    return {key: 0 if key == 'population' else None for key in LAYER_KEYS}


def city_layers(city):
    income = {f'h{h.id}': {'wage': 0, 'gross': 0, 'tax': 0} for h in city.residents}
    for payment in city.current.get('payments', []):
        recipient = income.get(payment['target'])
        if recipient is not None:
            if payment['purpose'] in WAGE_PURPOSES:
                recipient['wage'] += payment['cents']
            if payment['purpose'] in TAXABLE_INCOME:
                recipient['gross'] += payment['cents']
        if payment['purpose'] == 'income_tax' and payment['source'] in income:
            income[payment['source']]['tax'] += payment['cents']
    result = {}
    for node in range(35):
        households = [h for h in city.residents if h.node == node]
        count = len(households)
        settled = city.month > 0
        observed = settled and count > 0
        receipts = [income[f'h{h.id}'] for h in households]
        result[node] = {
            'population': count,
            'housing_rent': city.home_rent(node) / 100 if count or 'R' in RIGHTS[city.zones[node]] else None,
            'wage_income': sum(x['wage'] for x in receipts) / count / 100 if observed else None,
            'net_income': sum(x['gross'] - x['tax'] for x in receipts) / count / 100 if observed else None,
            'commute': sum(city.commute.get(h.id, 0) for h in households) / count if observed else None,
            'shortage': sum(4 - h.food - h.goods for h in households) / count if observed else None,
            'exposure': city.exposure(node) if settled else None,
        }
    return result
