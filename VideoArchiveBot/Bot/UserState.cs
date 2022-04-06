namespace VideoArchiveBot.Bot;

internal class UsersState : IDisposable
{
	public enum UserState
	{
		/// <summary>
		/// User should send the video to bot
		/// </summary>
		Video,

		/// <summary>
		/// User should send a number which says which session is this
		/// </summary>
		SessionNumber,

		/// <summary>
		/// User should send the date of the session to bot
		/// </summary>
		SessionDate,

		/// <summary>
		/// User should send the topic to bot
		/// </summary>
		Topic,

		/// <summary>
		/// No more operations is valid here
		/// </summary>
		Done,
	}

	private class UserStateData
	{
		/// <summary>
		/// When was this entry added to map
		/// </summary>
		public DateTime AddedTime { get; }

		/// <summary>
		/// The course ID which is the row ID of courses table
		/// </summary>
		public int CourseId { get; }

		/// <summary>
		/// What do we expect from user now?
		/// </summary>
		public UserState State { get; private set; }

		private string _videoId;

		/// <summary>
		/// Sets or gets the video ID which user has uploaded
		/// Setting this value cases the <see cref="State"/> to change to <see cref="UserState.SessionNumber"/>
		/// </summary>
		/// <exception cref="InvalidOperationException">When state is not <see cref="UserState.Video"/></exception>
		public string VideoId
		{
			get => _videoId;
			set
			{
				if (State != UserState.Video)
					throw new InvalidOperationException("State is not video");
				State = UserState.SessionNumber;
				_videoId = value;
			}
		}

		private int _sessionNumber;

		/// <summary>
		/// Sets or gets the session number of the uploaded video
		/// Setting this value cases the <see cref="State"/> to change to <see cref="UserState.SessionDate"/>
		/// </summary>
		/// <exception cref="InvalidOperationException">When state is not <see cref="UserState.SessionNumber"/></exception>
		public int SessionNumber
		{
			get => _sessionNumber;
			set
			{
				if (State != UserState.SessionNumber)
					throw new InvalidOperationException("State is not session number");
				State = UserState.SessionDate;
				_sessionNumber = value;
			}
		}

		private DateOnly _sessionDate;

		/// <summary>
		/// Sets or gets the session date of the uploaded video
		/// Setting this value cases the <see cref="State"/> to change to <see cref="UserState.Topic"/>
		/// </summary>
		/// <exception cref="InvalidOperationException">When state is not <see cref="UserState.SessionDate"/></exception>
		public DateOnly SessionDate
		{
			get => _sessionDate;
			set
			{
				if (State != UserState.SessionDate)
					throw new InvalidOperationException("State is not session date");
				State = UserState.Topic;
				_sessionDate = value;
			}
		}

		private string? _topic;

		/// <summary>
		/// Sets or gets the topic of the uploaded video
		/// Setting this value cases the <see cref="State"/> to change to <see cref="UserState.Done"/>
		/// </summary>
		/// <exception cref="InvalidOperationException">When state is not <see cref="UserState.Topic"/></exception>
		public string? Topic
		{
			get => _topic;
			set
			{
				if (State != UserState.Topic)
					throw new InvalidOperationException("State is not topic");
				State = UserState.Done;
				_topic = value;
			}
		}

		public UserStateData(int courseId)
		{
			AddedTime = DateTime.Now;
			CourseId = courseId;
			State = UserState.Video;
			_videoId = string.Empty;
		}
	}

	private readonly Dictionary<long, UserStateData> _states = new();
	private readonly CancellationTokenSource _cancelCleanUp = new();

	public UsersState()
	{
		// Cleanup the list as it goes...
		new Task(async () => { await CleanUp(); }).Start();
	}

	private async Task CleanUp()
	{
		// Setup the cancel token
		var cancelToken = _cancelCleanUp.Token;
		cancelToken.ThrowIfCancellationRequested();
		// Loop and cleanup
		while (true)
		{
			await Task.Delay(TimeSpan.FromMinutes(10), cancelToken);
			lock (_states)
				foreach (var state in _states.Where(state =>
					         DateTime.Now - state.Value.AddedTime > TimeSpan.FromHours(1)))
					_states.Remove(state.Key);
		}
		// ReSharper disable once FunctionNeverReturns
	}

	public void AddStateForUser(int courseId, long uploaderId)
	{
		lock (_states)
			_states[uploaderId] = new UserStateData(courseId);
	}

	/// <summary>
	/// Sets the video of a user in states
	/// </summary>
	/// <param name="userId">The user to change it's video</param>
	/// <param name="videoId">The video ID</param>
	/// <exception cref="KeyNotFoundException">When userID had no state</exception>
	/// <exception cref="InvalidOperationException">When state is not <see cref="UserState.Video"/></exception>
	public void SetVideo(long userId, string videoId)
	{
		lock (_states)
			_states[userId].VideoId = videoId;
	}

	/// <summary>
	/// Gets user's state
	/// </summary>
	/// <param name="userId">User ID to get it's state</param>
	/// <returns>The state</returns>
	/// <exception cref="KeyNotFoundException">When userID has no state</exception>
	public UserState GetUsersState(long userId)
	{
		lock (_states)
			return _states[userId].State;
	}

	public void SetSessionNumber(long userId, int sessionNumber)
	{
		lock (_states)
			_states[userId].SessionNumber = sessionNumber;
	}

	public void SetSessionDate(long userId, DateOnly date)
	{
		lock (_states)
			_states[userId].SessionDate = date;
	}

	public void SetSessionTopic(long userId, string? topic)
	{
		lock (_states)
			_states[userId].Topic = topic;
	}

	/// <summary>
	/// This method will get the <see cref="Database.Types.Video"/> in order to perform an insert operation from state.
	/// After invoking this function, the state of user will be deleted from map
	/// </summary>
	/// <param name="userId"></param>
	/// <returns></returns>
	public Database.Types.Video GetDatabaseTypeFromData(long userId)
	{
		// Remove from map
		UserStateData? data;
		lock (_states)
			if (!_states.Remove(userId, out data))
				throw new KeyNotFoundException();
		// Now create the type
		return new Database.Types.Video()
		{
			Uploader = userId,
			CourseID = data.CourseId,
			VideoFileID = data.VideoId,
			SessionNumber = data.SessionNumber,
			SessionDate = data.SessionDate.ToDateTime(TimeOnly.MinValue),
			Topic = data.Topic,
			AddedTime = DateTime.Now,
		};
	}

	/// <summary>
	/// Remove will unconditionally remove a key from states
	/// </summary>
	/// <param name="userId">The user to remove</param>
	public void Remove(long userId)
	{
		lock (_states)
			_states.Remove(userId);
	}

	public void Dispose()
	{
		_cancelCleanUp.Cancel();
	}
}