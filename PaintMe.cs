using System.Collections.Generic;
using UnityEngine;

public class PaintMe : MonoBehaviour
{
	public List<Material> mats = new List<Material>();

	public int materialPos;

	public bool alredyPerformed;

	private void Start()
	{
		if (!alredyPerformed)
		{
			MeshRenderer component = GetComponent<MeshRenderer>();
			Material[] sharedMaterials = component.sharedMaterials;
			Material material = mats[Random.Range(0, mats.Count)];
			sharedMaterials[materialPos] = material;
			component.sharedMaterials = sharedMaterials;
			alredyPerformed = true;
		}
	}
}
