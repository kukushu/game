using UnityEngine;

namespace HarborCity
{
    [CreateAssetMenu(menuName="HarborCity/Game Data/Economy")]
    public sealed class HarborCityEconomyAsset:ScriptableObject
    {
        [SerializeField] GameDesignData definition=new GameDesignData();
        public GameDesignData Snapshot()=>new CityGameDesign(definition).Snapshot();
        public static GameDesignData Load()
        {
            var asset=Resources.Load<HarborCityEconomyAsset>("GameData/Economy/DefaultEconomy");
            if(asset==null)throw new System.InvalidOperationException("Missing GameData/Economy/DefaultEconomy asset");
            return asset.Snapshot();
        }
    }
}
