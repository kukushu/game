using UnityEngine;

public static class UVs
{
	public static int[] numbers = new int[11]
	{
		0, 9, 10, 11, 12, 13, 14, 15, 16, 17,
		18
	};

	public static Vector3 p0 = new Vector3(-0.5f, -4f, 0.5f);

	public static Vector3 p1 = new Vector3(0.5f, -4f, 0.5f);

	public static Vector3 p2 = new Vector3(0.5f, -4f, -0.5f);

	public static Vector3 p3 = new Vector3(-0.5f, -4f, -0.5f);

	public static Vector3 p4 = new Vector3(-0.5f, 0.5f, 0.5f);

	public static Vector3 p5 = new Vector3(0.5f, 0.5f, 0.5f);

	public static Vector3 p6 = new Vector3(0.5f, 0.5f, -0.5f);

	public static Vector3 p7 = new Vector3(-0.5f, 0.5f, -0.5f);

	public static Vector3 p8 = new Vector3(0f, 0f, 0f);

	public static Vector3[] vTop = new Vector3[4] { p6, p5, p4, p7 };

	public static Vector3[] vLeft = new Vector3[4] { p7, p4, p0, p3 };

	public static Vector3[] vRight = new Vector3[4] { p5, p6, p2, p1 };

	public static Vector3[] vFront = new Vector3[4] { p4, p5, p1, p0 };

	public static Vector3[] vBack = new Vector3[4] { p6, p7, p3, p2 };

	public static Vector3[] nTop = new Vector3[4]
	{
		Vector3.up,
		Vector3.up,
		Vector3.up,
		Vector3.up
	};

	public static Vector3[] nLeft = new Vector3[4]
	{
		Vector3.left,
		Vector3.left,
		Vector3.left,
		Vector3.left
	};

	public static Vector3[] nRight = new Vector3[4]
	{
		Vector3.right,
		Vector3.right,
		Vector3.right,
		Vector3.right
	};

	public static Vector3[] nFront = new Vector3[4]
	{
		Vector3.forward,
		Vector3.forward,
		Vector3.forward,
		Vector3.forward
	};

	public static Vector3[] nBack = new Vector3[4]
	{
		Vector3.back,
		Vector3.back,
		Vector3.back,
		Vector3.back
	};

	public static int[] triangles = new int[6] { 3, 1, 0, 3, 2, 1 };

	public static int[] invertTriangles = new int[6] { 2, 0, 3, 2, 1, 0 };

	public static Vector2[,] blockUVsCity = new Vector2[19, 4]
	{
		{
			new Vector2(0f, 0.75f),
			new Vector2(0.25f, 0.75f),
			new Vector2(0.25f, 1f),
			new Vector2(0f, 1f)
		},
		{
			new Vector2(0.755f, 0.005f),
			new Vector2(0.995f, 0.005f),
			new Vector2(0.995f, 0.995f),
			new Vector2(0.755f, 0.995f)
		},
		{
			new Vector2(0f, 0f),
			new Vector2(0.25f, 0f),
			new Vector2(0.25f, 0.25f),
			new Vector2(0f, 0.25f)
		},
		{
			new Vector2(0.505f, 0.755f),
			new Vector2(0.745f, 0.755f),
			new Vector2(0.745f, 0.995f),
			new Vector2(0.505f, 0.995f)
		},
		{
			new Vector2(1f, 1f),
			new Vector2(0.5f, 1f),
			new Vector2(0.5f, 0.5f),
			new Vector2(1f, 0.5f)
		},
		{
			new Vector2(0f, 1f),
			new Vector2(0.25f, 1f),
			new Vector2(0.25f, 0.75f),
			new Vector2(0f, 0.75f)
		},
		{
			new Vector2(0f, 1f),
			new Vector2(0.25f, 1f),
			new Vector2(0.25f, 0.75f),
			new Vector2(0f, 0.75f)
		},
		{
			new Vector2(0f, 0f),
			new Vector2(0.25f, 0f),
			new Vector2(0.25f, 1f),
			new Vector2(0f, 1f)
		},
		{
			new Vector2(0.5f, 0f),
			new Vector2(0.75f, 0f),
			new Vector2(0.75f, 1f),
			new Vector2(0.5f, 1f)
		},
		{
			new Vector2(0.25f, 0.75f),
			new Vector2(0.5f, 0.75f),
			new Vector2(0.5f, 1f),
			new Vector2(0.25f, 1f)
		},
		{
			new Vector2(0f, 0.5f),
			new Vector2(0.25f, 0.5f),
			new Vector2(0.25f, 0.75f),
			new Vector2(0f, 0.75f)
		},
		{
			new Vector2(0.25f, 0.5f),
			new Vector2(0.5f, 0.5f),
			new Vector2(0.5f, 0.75f),
			new Vector2(0.25f, 0.75f)
		},
		{
			new Vector2(0f, 0.25f),
			new Vector2(0.25f, 0.25f),
			new Vector2(0.25f, 0.5f),
			new Vector2(0f, 0.5f)
		},
		{
			new Vector2(0.25f, 0.25f),
			new Vector2(0.5f, 0.25f),
			new Vector2(0.5f, 0.5f),
			new Vector2(0.25f, 0.5f)
		},
		{
			new Vector2(0f, 0f),
			new Vector2(0.25f, 0f),
			new Vector2(0.25f, 0.25f),
			new Vector2(0f, 0.25f)
		},
		{
			new Vector2(0.25f, 0f),
			new Vector2(0.5f, 0f),
			new Vector2(0.5f, 0.25f),
			new Vector2(0.25f, 0.25f)
		},
		{
			new Vector2(0.5f, 0.5f),
			new Vector2(0.75f, 0.5f),
			new Vector2(0.75f, 0.75f),
			new Vector2(0.5f, 0.75f)
		},
		{
			new Vector2(0.5f, 0.25f),
			new Vector2(0.75f, 0.25f),
			new Vector2(0.75f, 0.5f),
			new Vector2(0.5f, 0.5f)
		},
		{
			new Vector2(0.5f, 0f),
			new Vector2(0.75f, 0f),
			new Vector2(0.75f, 0.25f),
			new Vector2(0.5f, 0.25f)
		}
	};

	public static Vector2[,] blockUVs = new Vector2[14, 4]
	{
		{
			new Vector2(0f, 0.755f),
			new Vector2(0.245f, 0.755f),
			new Vector2(0.245f, 0.995f),
			new Vector2(0f, 0.995f)
		},
		{
			new Vector2(0.505f, 0.005f),
			new Vector2(0.62f, 0.005f),
			new Vector2(0.62f, 0.995f),
			new Vector2(0.505f, 0.995f)
		},
		{
			new Vector2(0f, 0f),
			new Vector2(0.25f, 0f),
			new Vector2(0.25f, 0.25f),
			new Vector2(0f, 0.25f)
		},
		{
			new Vector2(0.26f, 0.01f),
			new Vector2(0.49f, 0.01f),
			new Vector2(0.49f, 0.24f),
			new Vector2(0.26f, 0.24f)
		},
		{
			new Vector2(1f, 1f),
			new Vector2(0.5f, 1f),
			new Vector2(0.5f, 0.5f),
			new Vector2(1f, 0.5f)
		},
		{
			new Vector2(0f, 1f),
			new Vector2(0.25f, 1f),
			new Vector2(0.25f, 0.75f),
			new Vector2(0f, 0.75f)
		},
		{
			new Vector2(0f, 1f),
			new Vector2(0.25f, 1f),
			new Vector2(0.25f, 0.75f),
			new Vector2(0f, 0.75f)
		},
		{
			new Vector2(0.625f, 0f),
			new Vector2(0.75f, 0f),
			new Vector2(0.75f, 1f),
			new Vector2(0.625f, 1f)
		},
		{
			new Vector2(0.755f, 0.01f),
			new Vector2(0.875f, 0.01f),
			new Vector2(0.875f, 0.99f),
			new Vector2(0.755f, 0.99f)
		},
		{
			new Vector2(0.255f, 0.745f),
			new Vector2(0.495f, 0.745f),
			new Vector2(0.495f, 1f),
			new Vector2(0.255f, 1f)
		},
		{
			new Vector2(0.01f, 0.505f),
			new Vector2(0.245f, 0.505f),
			new Vector2(0.245f, 0.745f),
			new Vector2(0.01f, 0.745f)
		},
		{
			new Vector2(0.255f, 0.505f),
			new Vector2(0.495f, 0.505f),
			new Vector2(0.495f, 0.74f),
			new Vector2(0.255f, 0.74f)
		},
		{
			new Vector2(0.01f, 0.255f),
			new Vector2(0.245f, 0.255f),
			new Vector2(0.245f, 0.495f),
			new Vector2(0.01f, 0.495f)
		},
		{
			new Vector2(0.255f, 0.255f),
			new Vector2(0.495f, 0.255f),
			new Vector2(0.495f, 0.495f),
			new Vector2(0.255f, 0.495f)
		}
	};
}
