using System.Collections.Generic;
using UnityEngine;

namespace EntroPi;

[CreateAssetMenu(fileName = "CloudLayers", menuName = "EntroPi/Cloud Shadows/Cloud Layers", order = 1)]
public class CloudLayers : ScriptableObject
{
	[SerializeField]
	private List<CloudLayerData> m_LayerData = new List<CloudLayerData>();

	public IEnumerable<CloudLayerData> Layers
	{
		get
		{
			foreach (CloudLayerData layerDatum in m_LayerData)
			{
				yield return layerDatum;
			}
		}
	}

	public CloudLayerData this[int index]
	{
		get
		{
			CloudLayerData result = null;
			if (index >= 0 && index < m_LayerData.Count)
			{
				result = m_LayerData[index];
			}
			return result;
		}
	}

	public int LayerCount => m_LayerData.Count;
}
