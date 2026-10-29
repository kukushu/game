"""Normal game runtime: integrated manufacturing market, with history-preserving load.

LandSession/LandCity remain the explicit pooled research baseline. This boundary
also upgrades blank blueprints when they launch, without rewriting past payments.
"""
from .model import LandCity
from .planning import PlanningSession
from .session import LandSession, MODES


class GamePlanningSession(PlanningSession):
    def launch(self):
        return GameSession.load(super().launch().dump())


class GameSession(LandSession):
    @classmethod
    def new(cls,seed=1,preset='adaptive'):
        if preset=='blank':
            return GamePlanningSession.load(LandSession.new(seed,'blank').dump())
        if not isinstance(seed,int) or isinstance(seed,bool) or not 0<=seed<2**31 or preset not in MODES:
            raise ValueError('种子或选址机制无效')
        city=LandCity(seed)
        city.mode='adaptive' if preset=='labor' else preset
        city.labor_mode='market'
        for _ in range(6):city.step()
        return cls(city,LandCity.restore(city.snapshot()),preset=preset)

    @classmethod
    def load(cls,data):
        if data.get('app')=='CityEconomyLab Blueprint':
            return GamePlanningSession.load(data)
        result=super().load(data)
        for city in (result.player,result.reference):
            # The old pool did not define stable contracts: start without an
            # invented employer. Existing accounts, jobs, wages and traces stay intact.
            if city.labor_mode!='market':
                city.labor_mode='market'
                city.employers={}
                city.labor_result={}
        return result
