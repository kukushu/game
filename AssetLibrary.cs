using UnityEngine;

public class AssetLibrary : MonoBehaviour
{
	public static AssetLibrary assetLib;

	public GameObject[] grass;

	public GameObject[] tropical_leaves;

	public GameObject[] tropical_trunk;

	public GameObject[] temperate_leaves;

	public GameObject[] temperate_trunk;

	public GameObject[] arid_leaves;

	public GameObject[] arid_trunk;

	public GameObject[] LC_leaves;

	public GameObject[] LC_trunk;

	public GameObject[] UC_leaves;

	public GameObject[] UC_trunk;

	public GameObject[] roads_leaves;

	public GameObject[] roads_trunk;

	public GameObject farTrunk;

	public GameObject farLeaves;

	private void Start()
	{
		assetLib = this;
	}
}
