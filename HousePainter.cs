using UnityEngine;

public class HousePainter : MonoBehaviour
{
	public int matIndex;

	public Material alterMat;

	public void ChangePaint(Renderer rend)
	{
		if (rend != null && !(Random.value < 0.5f))
		{
			Material[] sharedMaterials = rend.sharedMaterials;
			sharedMaterials[matIndex] = alterMat;
			rend.sharedMaterials = sharedMaterials;
		}
	}
}
