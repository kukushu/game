using UnityEngine;

public class Connection
{
	public int Capacity;

	public int Use;

	public int roadType;

	public bool local = true;

	public Vector2Int entrancePos = new Vector2Int(-1, -1);

	public Vector2Int exitPos = new Vector2Int(-1, -1);
}
