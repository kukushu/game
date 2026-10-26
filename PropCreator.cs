using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PropCreator : MonoBehaviour
{
	public List<PropType> Props = new List<PropType>();

	public Transform buildingTransform;

	private void Start()
	{
		foreach (PropType item in Props.ToList())
		{
			if (item == null)
			{
				Props.Remove(item);
			}
		}
	}
}
