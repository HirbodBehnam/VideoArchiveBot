using System.Runtime.CompilerServices;

namespace VideoArchiveBot.Util;

/// <summary>
/// Async blocker is a simple barrier to asynchronously stop the code and continue if from an event
/// </summary>
/// <remarks>Each AsyncBlocker is one time use</remarks>
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

	/// <summary>
	/// Continue will lift the barrier where this class is awaited on
	/// </summary>
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