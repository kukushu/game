using UnityEngine;

public class ChunkMB : MonoBehaviour
{
	public ChunkRegion ownerRegion;

	public Chunk owner;

	public void SetRegionOwner(ChunkRegion o)
	{
		ownerRegion = o;
	}

	public void SetOwner(Chunk o)
	{
		owner = o;
	}
}
