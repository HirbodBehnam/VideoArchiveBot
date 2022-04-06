namespace VideoArchiveBot.Util;

/// <summary>
/// A simple LRU key only cache with custom size
/// </summary>
/// <remarks>This cache is thread safe</remarks>
/// <typeparam name="TK">The type of cache</typeparam>
internal class LruCache<TK> where TK : notnull
{
	private readonly LinkedList<TK> _list = new();
	private readonly Dictionary<TK, LinkedListNode<TK>> _map;
	private readonly object _lock = new();
	private int MaxSize { get; }

	/// <summary>
	/// Create a new LRU cache with specified max size
	/// </summary>
	/// <param name="maxSize">The maximum size of cache</param>
	public LruCache(int maxSize)
	{
		if (maxSize <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxSize));
		MaxSize = maxSize;
		_map = new Dictionary<TK, LinkedListNode<TK>>(maxSize + 1);
	}

	/// <summary>
	/// Add will add a key to cache, removing the oldest key if needed
	/// </summary>
	/// <param name="key">The key to add</param>
	/// <returns>True if key already exists in cache otherwise false</returns>
	public bool Add(TK key)
	{
		lock (_lock)
		{
			// At first, if the list contains this key, just move it up
			if (_map.TryGetValue(key, out var node))
			{
				_list.Remove(node);
				_list.AddFirst(node);
				return true;
			}

			// Otherwise add it to list
			node = _list.AddFirst(key);
			_map.Add(key, node);
			// Remove last element if needed
			if (_list.Count <= MaxSize)
				return false;
			var lastElement = _list.Last!.Value;
			_list.RemoveLast();
			_map.Remove(lastElement);
			return false;
		}
	}
}