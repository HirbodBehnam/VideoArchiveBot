using System.Runtime.CompilerServices;

namespace VideoArchiveBot.Util;

internal class AsyncBlocker : INotifyCompletion
{
	private Action? _continueFunction;

	public void OnCompleted(Action continuation)
	{
		if (IsCompleted)
			continuation();
		else
			_continueFunction = continuation;
	}

	public bool IsCompleted { get; private set; }

	public void GetResult()
	{
	}

	public void Continue()
	{
		IsCompleted = true;
		_continueFunction?.Invoke();
	}

	public AsyncBlocker GetAwaiter()
	{
		return this;
	}
}