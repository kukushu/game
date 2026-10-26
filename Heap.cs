public class Heap<T>
{
	private T[] items;

	private int currentItemCount;

	public Heap(int maxHeapSize)
	{
		items = new T[maxHeapSize];
	}
}
