using UnityEngine;

public class PropType : MonoBehaviour
{
	public enum Type
	{
		Grass,
		Tree,
		TreeOrProp,
		Prop,
		Front,
		Car,
		None
	}

	public Type type;
}
