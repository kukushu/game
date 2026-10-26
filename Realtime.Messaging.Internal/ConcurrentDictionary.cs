using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;

namespace Realtime.Messaging.Internal;

[Serializable]
[ComVisible(false)]
[DebuggerDisplay("Count = {Count}")]
public class ConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary, ICollection
{
	private class Node
	{
		internal TKey m_key;

		internal TValue m_value;

		internal volatile Node m_next;

		internal int m_hashcode;

		internal Node(TKey key, TValue value, int hashcode)
			: this(key, value, hashcode, (Node)null)
		{
		}

		internal Node(TKey key, TValue value, int hashcode, Node next)
		{
			m_key = key;
			m_value = value;
			m_next = next;
			m_hashcode = hashcode;
		}
	}

	private class DictionaryEnumerator : IDictionaryEnumerator, IEnumerator
	{
		private IEnumerator<KeyValuePair<TKey, TValue>> m_enumerator;

		public DictionaryEntry Entry => new DictionaryEntry(m_enumerator.Current.Key, m_enumerator.Current.Value);

		public object Key => m_enumerator.Current.Key;

		public object Value => m_enumerator.Current.Value;

		public object Current => Entry;

		internal DictionaryEnumerator(ConcurrentDictionary<TKey, TValue> dictionary)
		{
			m_enumerator = dictionary.GetEnumerator();
		}

		public bool MoveNext()
		{
			return m_enumerator.MoveNext();
		}

		public void Reset()
		{
			m_enumerator.Reset();
		}
	}

	[NonSerialized]
	private volatile Node[] m_buckets;

	[NonSerialized]
	private object[] m_locks;

	[NonSerialized]
	private volatile int[] m_countPerLock;

	private IEqualityComparer<TKey> m_comparer;

	private KeyValuePair<TKey, TValue>[] m_serializationArray;

	private int m_serializationConcurrencyLevel;

	private int m_serializationCapacity;

	private const int DEFAULT_CONCURRENCY_MULTIPLIER = 4;

	private const int DEFAULT_CAPACITY = 31;

	public TValue this[TKey key]
	{
		get
		{
			if (!TryGetValue(key, out var value))
			{
				throw new KeyNotFoundException();
			}
			return value;
		}
		set
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			TryAddInternal(key, value, updateIfExists: true, acquireLock: true, out var _);
		}
	}

	public int Count
	{
		get
		{
			int num = 0;
			int locksAcquired = 0;
			try
			{
				AcquireAllLocks(ref locksAcquired);
				for (int i = 0; i < m_countPerLock.Length; i++)
				{
					num += m_countPerLock[i];
				}
				return num;
			}
			finally
			{
				ReleaseLocks(0, locksAcquired);
			}
		}
	}

	public bool IsEmpty
	{
		get
		{
			int locksAcquired = 0;
			try
			{
				AcquireAllLocks(ref locksAcquired);
				for (int i = 0; i < m_countPerLock.Length; i++)
				{
					if (m_countPerLock[i] != 0)
					{
						return false;
					}
				}
			}
			finally
			{
				ReleaseLocks(0, locksAcquired);
			}
			return true;
		}
	}

	public ICollection<TKey> Keys => GetKeys();

	public ICollection<TValue> Values => GetValues();

	bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

	bool IDictionary.IsFixedSize => false;

	bool IDictionary.IsReadOnly => false;

	ICollection IDictionary.Keys => GetKeys();

	ICollection IDictionary.Values => GetValues();

	object IDictionary.this[object key]
	{
		get
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			if (key is TKey && TryGetValue((TKey)key, out var value))
			{
				return value;
			}
			return null;
		}
		set
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			if (!(key is TKey))
			{
				throw new ArgumentException(GetResource("ConcurrentDictionary_TypeOfKeyIncorrect"));
			}
			if (!(value is TValue))
			{
				throw new ArgumentException(GetResource("ConcurrentDictionary_TypeOfValueIncorrect"));
			}
			this[(TKey)key] = (TValue)value;
		}
	}

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	private static int DefaultConcurrencyLevel => 4 * Environment.ProcessorCount;

	public ConcurrentDictionary()
		: this(DefaultConcurrencyLevel, 31)
	{
	}

	public ConcurrentDictionary(int concurrencyLevel, int capacity)
		: this(concurrencyLevel, capacity, (IEqualityComparer<TKey>)EqualityComparer<TKey>.Default)
	{
	}

	public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
		: this(collection, (IEqualityComparer<TKey>)EqualityComparer<TKey>.Default)
	{
	}

	public ConcurrentDictionary(IEqualityComparer<TKey> comparer)
		: this(DefaultConcurrencyLevel, 31, comparer)
	{
	}

	public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
		: this(DefaultConcurrencyLevel, collection, comparer)
	{
	}

	public ConcurrentDictionary(int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
		: this(concurrencyLevel, 31, comparer)
	{
		if (collection == null)
		{
			throw new ArgumentNullException("collection");
		}
		if (comparer == null)
		{
			throw new ArgumentNullException("comparer");
		}
		InitializeFromCollection(collection);
	}

	private void InitializeFromCollection(IEnumerable<KeyValuePair<TKey, TValue>> collection)
	{
		foreach (KeyValuePair<TKey, TValue> item in collection)
		{
			if (item.Key == null)
			{
				throw new ArgumentNullException("key");
			}
			if (!TryAddInternal(item.Key, item.Value, updateIfExists: false, acquireLock: false, out var _))
			{
				throw new ArgumentException(GetResource("ConcurrentDictionary_SourceContainsDuplicateKeys"));
			}
		}
	}

	public ConcurrentDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> comparer)
	{
		if (concurrencyLevel < 1)
		{
			throw new ArgumentOutOfRangeException("concurrencyLevel", GetResource("ConcurrentDictionary_ConcurrencyLevelMustBePositive"));
		}
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException("capacity", GetResource("ConcurrentDictionary_CapacityMustNotBeNegative"));
		}
		if (comparer == null)
		{
			throw new ArgumentNullException("comparer");
		}
		if (capacity < concurrencyLevel)
		{
			capacity = concurrencyLevel;
		}
		m_locks = new object[concurrencyLevel];
		for (int i = 0; i < m_locks.Length; i++)
		{
			m_locks[i] = new object();
		}
		m_countPerLock = new int[m_locks.Length];
		m_buckets = new Node[capacity];
		m_comparer = comparer;
	}

	public bool TryAdd(TKey key, TValue value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		TValue resultingValue;
		return TryAddInternal(key, value, updateIfExists: false, acquireLock: true, out resultingValue);
	}

	public bool ContainsKey(TKey key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		TValue value;
		return TryGetValue(key, out value);
	}

	public bool TryRemove(TKey key, out TValue value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		return TryRemoveInternal(key, out value, matchValue: false, default);
	}

	private bool TryRemoveInternal(TKey key, out TValue value, bool matchValue, TValue oldValue)
	{
		while (true)
		{
			Node[] buckets = m_buckets;
			GetBucketAndLockNo(m_comparer.GetHashCode(key), out var bucketNo, out var lockNo, buckets.Length);
			lock (m_locks[lockNo])
			{
				if (buckets != m_buckets)
				{
					continue;
				}
				Node node = null;
				for (Node node2 = m_buckets[bucketNo]; node2 != null; node2 = node2.m_next)
				{
					if (m_comparer.Equals(node2.m_key, key))
					{
						if (matchValue && !EqualityComparer<TValue>.Default.Equals(oldValue, node2.m_value))
						{
							value = default;
							return false;
						}
						if (node == null)
						{
							m_buckets[bucketNo] = node2.m_next;
						}
						else
						{
							node.m_next = node2.m_next;
						}
						value = node2.m_value;
						m_countPerLock[lockNo]--;
						return true;
					}
					node = node2;
				}
				break;
			}
		}
		value = default;
		return false;
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		Node[] buckets = m_buckets;
		GetBucketAndLockNo(m_comparer.GetHashCode(key), out var bucketNo, out var _, buckets.Length);
		Node node = buckets[bucketNo];
		Thread.MemoryBarrier();
		while (node != null)
		{
			if (m_comparer.Equals(node.m_key, key))
			{
				value = node.m_value;
				return true;
			}
			node = node.m_next;
		}
		value = default;
		return false;
	}

	public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		int hashCode = m_comparer.GetHashCode(key);
		IEqualityComparer<TValue> equalityComparer = EqualityComparer<TValue>.Default;
		while (true)
		{
			Node[] buckets = m_buckets;
			GetBucketAndLockNo(hashCode, out var bucketNo, out var lockNo, buckets.Length);
			lock (m_locks[lockNo])
			{
				if (buckets != m_buckets)
				{
					continue;
				}
				Node node = null;
				for (Node node2 = buckets[bucketNo]; node2 != null; node2 = node2.m_next)
				{
					if (m_comparer.Equals(node2.m_key, key))
					{
						if (equalityComparer.Equals(node2.m_value, comparisonValue))
						{
							Node node3 = new Node(node2.m_key, newValue, hashCode, node2.m_next);
							if (node == null)
							{
								buckets[bucketNo] = node3;
							}
							else
							{
								node.m_next = node3;
							}
							return true;
						}
						return false;
					}
					node = node2;
				}
				return false;
			}
		}
	}

	public void Clear()
	{
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			m_buckets = new Node[31];
			Array.Clear(m_countPerLock, 0, m_countPerLock.Length);
		}
		finally
		{
			ReleaseLocks(0, locksAcquired);
		}
	}

	void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", GetResource("ConcurrentDictionary_IndexIsNegative"));
		}
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			int num = 0;
			for (int i = 0; i < m_locks.Length; i++)
			{
				num += m_countPerLock[i];
			}
			if (array.Length - num < index || num < 0)
			{
				throw new ArgumentException(GetResource("ConcurrentDictionary_ArrayNotLargeEnough"));
			}
			CopyToPairs(array, index);
		}
		finally
		{
			ReleaseLocks(0, locksAcquired);
		}
	}

	public KeyValuePair<TKey, TValue>[] ToArray()
	{
		int locksAcquired = 0;
		checked
		{
			try
			{
				AcquireAllLocks(ref locksAcquired);
				int num = 0;
				for (int i = 0; i < m_locks.Length; i++)
				{
					num += m_countPerLock[i];
				}
				KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[num];
				CopyToPairs(array, 0);
				return array;
			}
			finally
			{
				ReleaseLocks(0, locksAcquired);
			}
		}
	}

	private void CopyToPairs(KeyValuePair<TKey, TValue>[] array, int index)
	{
		Node[] buckets = m_buckets;
		for (int i = 0; i < buckets.Length; i++)
		{
			for (Node node = buckets[i]; node != null; node = node.m_next)
			{
				array[index] = new KeyValuePair<TKey, TValue>(node.m_key, node.m_value);
				index++;
			}
		}
	}

	private void CopyToEntries(DictionaryEntry[] array, int index)
	{
		Node[] buckets = m_buckets;
		for (int i = 0; i < buckets.Length; i++)
		{
			for (Node node = buckets[i]; node != null; node = node.m_next)
			{
				array[index] = new DictionaryEntry(node.m_key, node.m_value);
				index++;
			}
		}
	}

	private void CopyToObjects(object[] array, int index)
	{
		Node[] buckets = m_buckets;
		for (int i = 0; i < buckets.Length; i++)
		{
			for (Node node = buckets[i]; node != null; node = node.m_next)
			{
				array[index] = new KeyValuePair<TKey, TValue>(node.m_key, node.m_value);
				index++;
			}
		}
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		Node[] buckets = m_buckets;
		for (int i = 0; i < buckets.Length; i++)
		{
			Node current = buckets[i];
			Thread.MemoryBarrier();
			while (current != null)
			{
				yield return new KeyValuePair<TKey, TValue>(current.m_key, current.m_value);
				current = current.m_next;
			}
		}
	}

	private bool TryAddInternal(TKey key, TValue value, bool updateIfExists, bool acquireLock, out TValue resultingValue)
	{
		int hashCode = m_comparer.GetHashCode(key);
		Node[] buckets;
		bool flag;
		while (true)
		{
			buckets = m_buckets;
			GetBucketAndLockNo(hashCode, out var bucketNo, out var lockNo, buckets.Length);
			flag = false;
			bool flag2 = false;
			try
			{
				if (acquireLock)
				{
					Monitor.Enter(m_locks[lockNo]);
					flag2 = true;
				}
				if (buckets != m_buckets)
				{
					continue;
				}
				Node node = null;
				for (Node node2 = buckets[bucketNo]; node2 != null; node2 = node2.m_next)
				{
					if (m_comparer.Equals(node2.m_key, key))
					{
						if (updateIfExists)
						{
							Node node3 = new Node(node2.m_key, value, hashCode, node2.m_next);
							if (node == null)
							{
								buckets[bucketNo] = node3;
							}
							else
							{
								node.m_next = node3;
							}
							resultingValue = value;
						}
						else
						{
							resultingValue = node2.m_value;
						}
						return false;
					}
					node = node2;
				}
				buckets[bucketNo] = new Node(key, value, hashCode, buckets[bucketNo]);
				checked
				{
					m_countPerLock[lockNo]++;
				}
				if (m_countPerLock[lockNo] > buckets.Length / m_locks.Length)
				{
					flag = true;
				}
				break;
			}
			finally
			{
				if (flag2)
				{
					Monitor.Exit(m_locks[lockNo]);
				}
			}
		}
		if (flag)
		{
			GrowTable(buckets);
		}
		resultingValue = value;
		return true;
	}

	public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (valueFactory == null)
		{
			throw new ArgumentNullException("valueFactory");
		}
		if (TryGetValue(key, out var value))
		{
			return value;
		}
		TryAddInternal(key, valueFactory(key), updateIfExists: false, acquireLock: true, out value);
		return value;
	}

	public TValue GetOrAdd(TKey key, TValue value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		TryAddInternal(key, value, updateIfExists: false, acquireLock: true, out var resultingValue);
		return resultingValue;
	}

	public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (addValueFactory == null)
		{
			throw new ArgumentNullException("addValueFactory");
		}
		if (updateValueFactory == null)
		{
			throw new ArgumentNullException("updateValueFactory");
		}
		TValue resultingValue;
		while (true)
		{
			if (TryGetValue(key, out var value))
			{
				TValue val = updateValueFactory(key, value);
				if (TryUpdate(key, val, value))
				{
					return val;
				}
			}
			else
			{
				TValue val = addValueFactory(key);
				if (TryAddInternal(key, val, updateIfExists: false, acquireLock: true, out resultingValue))
				{
					break;
				}
			}
		}
		return resultingValue;
	}

	public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (updateValueFactory == null)
		{
			throw new ArgumentNullException("updateValueFactory");
		}
		TValue resultingValue;
		while (true)
		{
			if (TryGetValue(key, out var value))
			{
				TValue val = updateValueFactory(key, value);
				if (TryUpdate(key, val, value))
				{
					return val;
				}
			}
			else if (TryAddInternal(key, addValue, updateIfExists: false, acquireLock: true, out resultingValue))
			{
				break;
			}
		}
		return resultingValue;
	}

	void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
	{
		if (!TryAdd(key, value))
		{
			throw new ArgumentException(GetResource("ConcurrentDictionary_KeyAlreadyExisted"));
		}
	}

	bool IDictionary<TKey, TValue>.Remove(TKey key)
	{
		TValue value;
		return TryRemove(key, out value);
	}

	void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
	{
		((IDictionary<TKey, TValue>)this).Add(keyValuePair.Key, keyValuePair.Value);
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
	{
		if (!TryGetValue(keyValuePair.Key, out var value))
		{
			return false;
		}
		return EqualityComparer<TValue>.Default.Equals(value, keyValuePair.Value);
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
	{
		if (keyValuePair.Key == null)
		{
			throw new ArgumentNullException(GetResource("ConcurrentDictionary_ItemKeyIsNull"));
		}
		TValue value;
		return TryRemoveInternal(keyValuePair.Key, out value, matchValue: true, keyValuePair.Value);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	void IDictionary.Add(object key, object value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (!(key is TKey))
		{
			throw new ArgumentException(GetResource("ConcurrentDictionary_TypeOfKeyIncorrect"));
		}
		TValue value2;
		try
		{
			value2 = (TValue)value;
		}
		catch (InvalidCastException)
		{
			throw new ArgumentException(GetResource("ConcurrentDictionary_TypeOfValueIncorrect"));
		}
		((IDictionary<TKey, TValue>)this).Add((TKey)key, value2);
	}

	bool IDictionary.Contains(object key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (key is TKey)
		{
			return ContainsKey((TKey)key);
		}
		return false;
	}

	IDictionaryEnumerator IDictionary.GetEnumerator()
	{
		return new DictionaryEnumerator(this);
	}

	void IDictionary.Remove(object key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (key is TKey)
		{
			TryRemove((TKey)key, out var _);
		}
	}

	void ICollection.CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", GetResource("ConcurrentDictionary_IndexIsNegative"));
		}
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			int num = 0;
			for (int i = 0; i < m_locks.Length; i++)
			{
				num += m_countPerLock[i];
			}
			if (array.Length - num < index || num < 0)
			{
				throw new ArgumentException(GetResource("ConcurrentDictionary_ArrayNotLargeEnough"));
			}
			if (array is KeyValuePair<TKey, TValue>[] array2)
			{
				CopyToPairs(array2, index);
				return;
			}
			if (array is DictionaryEntry[] array3)
			{
				CopyToEntries(array3, index);
				return;
			}
			if (array is object[] array4)
			{
				CopyToObjects(array4, index);
				return;
			}
			throw new ArgumentException(GetResource("ConcurrentDictionary_ArrayIncorrectType"), "array");
		}
		finally
		{
			ReleaseLocks(0, locksAcquired);
		}
	}

	private void GrowTable(Node[] buckets)
	{
		int locksAcquired = 0;
		checked
		{
			try
			{
				AcquireLocks(0, 1, ref locksAcquired);
				if (buckets != m_buckets)
				{
					return;
				}
				int i;
				try
				{
					for (i = buckets.Length * 2 + 1; unchecked(i % 3 == 0 || i % 5 == 0 || i % 7 == 0); i += 2)
					{
					}
				}
				catch (OverflowException)
				{
					return;
				}
				Node[] array = new Node[i];
				int[] array2 = new int[m_locks.Length];
				AcquireLocks(1, m_locks.Length, ref locksAcquired);
				for (int j = 0; j < buckets.Length; j = unchecked(j + 1))
				{
					Node node = buckets[j];
					while (node != null)
					{
						Node next = node.m_next;
						GetBucketAndLockNo(node.m_hashcode, out var bucketNo, out var lockNo, array.Length);
						array[bucketNo] = new Node(node.m_key, node.m_value, node.m_hashcode, array[bucketNo]);
						array2[lockNo]++;
						node = next;
					}
				}
				m_buckets = array;
				m_countPerLock = array2;
			}
			finally
			{
				ReleaseLocks(0, locksAcquired);
			}
		}
	}

	private void GetBucketAndLockNo(int hashcode, out int bucketNo, out int lockNo, int bucketCount)
	{
		bucketNo = (hashcode & 0x7FFFFFFF) % bucketCount;
		lockNo = bucketNo % m_locks.Length;
	}

	private void AcquireAllLocks(ref int locksAcquired)
	{
		AcquireLocks(0, m_locks.Length, ref locksAcquired);
	}

	private void AcquireLocks(int fromInclusive, int toExclusive, ref int locksAcquired)
	{
		for (int i = fromInclusive; i < toExclusive; i++)
		{
			bool flag = false;
			try
			{
				Monitor.Enter(m_locks[i]);
				flag = true;
			}
			finally
			{
				if (flag)
				{
					locksAcquired++;
				}
			}
		}
	}

	private void ReleaseLocks(int fromInclusive, int toExclusive)
	{
		for (int i = fromInclusive; i < toExclusive; i++)
		{
			Monitor.Exit(m_locks[i]);
		}
	}

	private ReadOnlyCollection<TKey> GetKeys()
	{
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			List<TKey> list = new List<TKey>();
			for (int i = 0; i < m_buckets.Length; i++)
			{
				for (Node node = m_buckets[i]; node != null; node = node.m_next)
				{
					list.Add(node.m_key);
				}
			}
			return new ReadOnlyCollection<TKey>(list);
		}
		finally
		{
			ReleaseLocks(0, locksAcquired);
		}
	}

	private ReadOnlyCollection<TValue> GetValues()
	{
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			List<TValue> list = new List<TValue>();
			for (int i = 0; i < m_buckets.Length; i++)
			{
				for (Node node = m_buckets[i]; node != null; node = node.m_next)
				{
					list.Add(node.m_value);
				}
			}
			return new ReadOnlyCollection<TValue>(list);
		}
		finally
		{
			ReleaseLocks(0, locksAcquired);
		}
	}

	[Conditional("DEBUG")]
	private void Assert(bool condition)
	{
		if (!condition)
		{
			throw new Exception("Assertion failed.");
		}
	}

	private string GetResource(string key)
	{
		return key;
	}

	[OnSerializing]
	private void OnSerializing(StreamingContext context)
	{
		m_serializationArray = ToArray();
		m_serializationConcurrencyLevel = m_locks.Length;
		m_serializationCapacity = m_buckets.Length;
	}

	[OnDeserialized]
	private void OnDeserialized(StreamingContext context)
	{
		KeyValuePair<TKey, TValue>[] serializationArray = m_serializationArray;
		m_buckets = new Node[m_serializationCapacity];
		m_countPerLock = new int[m_serializationConcurrencyLevel];
		m_locks = new object[m_serializationConcurrencyLevel];
		for (int i = 0; i < m_locks.Length; i++)
		{
			m_locks[i] = new object();
		}
		InitializeFromCollection(serializationArray);
		m_serializationArray = null;
	}
}
