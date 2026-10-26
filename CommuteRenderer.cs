using System.Collections.Generic;
using UnityEngine;

public class CommuteRenderer : MonoBehaviour
{
	public LineRenderer line;

	public Material lineMaterial;

	private void Start()
	{
		line = GetComponent<LineRenderer>();
	}

	public void SetUpLine(List<Vector3> trans, Color lineColor)
	{
		lineMaterial.color = lineColor;
		line.startWidth = 0.5f;
		line.endWidth = 0.5f;
		line.positionCount = trans.Count;
		line.SetPositions(trans.ToArray());
	}

	public void DestroyLine()
	{
		line.positionCount = 0;
	}
}
