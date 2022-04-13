using NUnit.Framework;
using VideoArchiveBot.Util;

namespace Tests;

public class LruCacheTest
{
	private const int MaxSize = 10;

	private static LruCache<int> GenerateAndPopulateCache()
	{
		var cache = new LruCache<int>(MaxSize);
		for (var i = 0; i < MaxSize; i++)
			Assert.False(cache.Add(i));
		return cache;
	}

	[Test]
	public void AddReplaceTest()
	{
		var cache = GenerateAndPopulateCache();
		// Cache is now full. Next entries will be thrown out
		Assert.False(cache.Add(MaxSize + 1)); // now zero must be thrown out
		Assert.False(cache.Add(0)); // this should return false because zero does not exists
	}

	[Test]
	public void AddMoveReplaceTest()
	{
		var cache = GenerateAndPopulateCache();
		Assert.True(cache.Add(0)); // By accessing 0 we must move it to front
		Assert.False(cache.Add(MaxSize + 1)); // We should throw out 1 not zero
		Assert.False(cache.Add(1));
	}
}