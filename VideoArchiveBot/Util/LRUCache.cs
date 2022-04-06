namespace VideoArchiveBot.Util;

internal class LruCache<TK> where TK : notnull
{
	private readonly LinkedList<TK> _list = new();
	private readonly Dictionary<TK, LinkedListNode<TK>> _map;
	private readonly object _lock = new();
	private int MaxSize { get; }

	public LruCache(int maxSize)
	{
		MaxSize = maxSize;
		_map = new Dictionary<TK, LinkedListNode<TK>>(maxSize);
	}

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