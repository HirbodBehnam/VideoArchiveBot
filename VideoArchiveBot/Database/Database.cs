using System.Text;
using SQLite;

namespace VideoArchiveBot.Database;

internal static class Database
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	private static SQLiteAsyncConnection _db;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	private static readonly Util.LruCache<long> UserCache = new(1000);

	public static async Task OpenConnection(string path)
	{
		_db = new SQLiteAsyncConnection(path);
		await _db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS courses (
	id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	name TEXT NOT NULL,
	lecturer TEXT NOT NULL,
	group_id INTEGER NOT NULL,
	course_id INTEGER NOT NULL
)");
		await _db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS users (
	user_id INTEGER PRIMARY KEY NOT NULL,
	username TEXT,
	name TEXT NOT NULL,
	is_admin INTEGER NOT NULL DEFAULT 0
)");
		await _db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS videos (
	id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	course_id INTEGER NOT NULL,
	video_file_id TEXT NOT NULL,
	session_number INTEGER NOT NULL,
	uploader INTEGER NOT NULL,
	topic TEXT,
	session_date BIGINT NOT NULL,
	added_time BIGINT NOT NULL,
	verified INTEGER DEFAULT 0,
	FOREIGN KEY(course_id) REFERENCES courses(id),
	FOREIGN KEY(uploader) REFERENCES users(user_id)
)");
	}

	public static async Task CloseDatabase()
	{
		await _db.CloseAsync();
	}

	/// <summary>
	/// This function will get a list of courses basic infos from database based on a pivot ID
	/// </summary>
	/// <param name="pivot">If pivot is <see cref="HelperTypes.Pivot.Up"/> courses with IDs more or equal than ID will show up in result
	/// Otherwise, all elements have less than or equal ID to id</param>
	/// <param name="id">The ID to get info around it. This ID is always included in the result</param>
	/// <param name="limit">Limit the number of elements in returned data</param>
	/// <returns>Returns three values
	/// At first returns the rows
	/// Second bool indicates if there are rows with smaller IDs
	/// Last bool indicates if there are rows with bigger IDs
	/// </returns>
	public static async Task<(List<Types.CourseBasicInfo> data, bool hasBefore, bool hasNext)> GetCoursesNames(
		HelperTypes.Pivot pivot, int id, int limit)
	{
		// At first get the requested data
		List<Types.CourseBasicInfo> data;
		if (pivot == HelperTypes.Pivot.Up)
			data = await _db.QueryAsync<Types.CourseBasicInfo>(
				"SELECT id, name, group_id FROM courses WHERE id >= ? ORDER BY id LIMIT ?", id, limit);
		else
		{
			data = await _db.QueryAsync<Types.CourseBasicInfo>(
				"SELECT id, name, group_id FROM courses WHERE id <= ? ORDER BY id DESC LIMIT ?", id, limit);
			data.Reverse(); // Get them ascending
		}

		// Now check if there is more data before
		// We simply use the data to get the IDs
		// We should never reach data.Count > 0 == false
		bool hasBefore = data.Count > 0 &&
		                 await _db.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM courses WHERE id < ?)",
			                 data[0].ID);
		bool hasAfter = data.Count > 0 &&
		                await _db.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM courses WHERE id > ?)",
			                data.Last().ID);
		return (data, hasBefore, hasAfter);
	}

	/// <summary>
	/// This function gets the videos of a course with pagination
	/// The code in it is very similar to <see cref="Database.GetCoursesNames"/>
	/// </summary>
	/// <param name="pivot">If pivot is <see cref="HelperTypes.Pivot.Up"/> courses with IDs more or equal than ID will show up in result
	/// Otherwise, all elements have less than or equal ID to id</param>
	/// <param name="sessionId">The session ID to get info around it. This ID is always included in the result</param>
	/// <param name="courseId">The course ID of the videos which we must get</param>
	/// <param name="limit">Limit the number of elements in returned data</param>
	/// <returns>Returns three values
	/// At first returns the rows
	/// Second bool indicates if there are rows with smaller IDs
	/// Last bool indicates if there are rows with bigger IDs
	/// </returns>
	public static async Task<(List<Types.VideoSessionInfo> data, bool hasBefore, bool hasNext)> GetVerifiedCourseVideos(
		HelperTypes.Pivot pivot, int sessionId, int courseId, int limit)
	{
		// At first get the requested data
		List<Types.VideoSessionInfo> data;
		if (pivot == HelperTypes.Pivot.Up)
			data = await _db.QueryAsync<Types.VideoSessionInfo>(
				"SELECT id, session_number, course_id FROM videos WHERE session_number >= ? AND course_id=? AND verified=TRUE ORDER BY session_number LIMIT ?",
				sessionId, courseId, limit);
		else
		{
			data = await _db.QueryAsync<Types.VideoSessionInfo>(
				"SELECT id, session_number, course_id FROM videos WHERE session_number <= ? AND course_id=? AND verified=TRUE ORDER BY session_number DESC LIMIT ?",
				sessionId, courseId, limit);
			data.Reverse(); // Get them ascending
		}

		// Now check if there is more data before
		// We simply use the data to get the IDs
		// We should never reach data.Count > 0 == false
		bool hasBefore = data.Count > 0 &&
		                 await _db.ExecuteScalarAsync<bool>(
			                 "SELECT EXISTS(SELECT 1 FROM videos WHERE course_id=? AND session_number < ? AND verified=TRUE)",
			                 courseId, data[0].SessionNumber);
		bool hasAfter = data.Count > 0 &&
		                await _db.ExecuteScalarAsync<bool>(
			                "SELECT EXISTS(SELECT 1 FROM videos WHERE course_id=? AND session_number > ? AND verified=TRUE)",
			                courseId, data.Last().SessionNumber);
		return (data, hasBefore, hasAfter);
	}

	public static async Task<List<Types.VideoTopicInfo>> GetVerifiedVideoTopics(int courseId)
	{
		return await _db.QueryAsync<Types.VideoTopicInfo>(
			"SELECT id, session_number, topic FROM videos WHERE course_id=? ORDER BY session_number", courseId);
	}

	/// <summary>
	/// This function will simply get a coursed based on it's database ID
	/// </summary>
	/// <param name="dbId">The <see cref="Types.Course.ID"/></param>
	public static async Task<Types.Course> GetCourse(int dbId)
	{
		return await _db.Table<Types.Course>().Where(db => db.ID == dbId).FirstAsync();
	}

	/// <summary>
	/// This function will add a video entry to video table
	/// </summary>
	/// <param name="video">The video to add</param>
	/// <returns>If the uploader was admin or not.</returns>
	public static async Task<bool> AddVideo(Types.Video video)
	{
		video.Verified = await IsAdmin(video.Uploader);
		await _db.InsertAsync(video);
		return video.Verified;
	}

	/// <summary>
	/// Register user will put the user in database if it does not exists in cache
	/// If user exists in cache, it will not touch the database
	/// If user does not exists in cache but exists in database, it will update the entries in database
	/// </summary>
	/// <param name="user">The user to update</param>
	public static async Task RegisterUser(Telegram.Bot.Types.User user)
	{
		// Check cache
		if (UserCache.Add(user.Id))
			return; // data exists in cache. No need to change database

		// Add to database (or update)
		await _db.ExecuteAsync(
			"REPLACE INTO users (user_id, username, name, is_admin) VALUES (?,?,?, IFNULL((SELECT is_admin FROM users WHERE user_id=?), 0))",
			user.Id, user.Username, user.FirstName + (user.LastName == null ? string.Empty : " " + user.LastName),
			user.Id);
	}

	/// <summary>
	/// Is admin checks if user is admin or not
	/// </summary>
	/// <param name="user">The user to check</param>
	/// <returns>True if admin otherwise false</returns>
	public static async Task<bool> IsAdmin(Telegram.Bot.Types.User user)
	{
		return await IsAdmin(user.Id);
	}

	/// <summary>
	/// Is admin checks if user is admin or not
	/// </summary>
	/// <param name="userId">The userId to check</param>
	/// <returns>True if admin otherwise false</returns>
	public static async Task<bool> IsAdmin(long userId)
	{
		return await _db.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM users WHERE user_id=? AND is_admin=1)",
			userId);
	}

	/// <summary>
	/// This method will get a string prettied list of unverified videos to give to admins to check
	/// The returned list is sorted by newest videos uploaded first
	/// Always the first 20 videos is returned
	/// </summary>
	/// <returns>A string which can be send to admins that contains the list of videos</returns>
	public static async Task<string> GetUnverifiedVideosList()
	{
		var videos =
			await _db.QueryAsync<Types.GetVideoResult>(
				"SELECT videos.id as video_id, video_file_id, session_number, session_date, topic, added_time, courses.name || ' G' || courses.group_id as course, users.username, users.name FROM videos INNER JOIN courses ON videos.course_id = courses.id INNER JOIN users ON videos.uploader = users.user_id WHERE verified=FALSE ORDER BY video_id DESC LIMIT 20");
		StringBuilder sb = new(videos.Count * 100);
		foreach (var video in videos)
			sb.AppendLine(video.ToReviewString());
		return sb.ToString();
	}

	/// <summary>
	/// Gets one video by it's database ID
	/// It contains all info which a video needs
	/// </summary>
	/// <param name="databaseId">The row ID</param>
	/// <param name="verified">Should the video we fetch be verified or not</param>
	/// <returns>Video data</returns>
	public static async Task<Types.GetVideoResult?> GetVideo(int databaseId, bool verified = true)
	{
		var data = await _db.QueryAsync<Types.GetVideoResult>(
			"SELECT videos.id as video_id, video_file_id, session_number, session_date, topic, added_time, courses.name || ' G' || courses.group_id as course, users.username, users.name FROM videos INNER JOIN courses ON videos.course_id = courses.id INNER JOIN users ON videos.uploader = users.user_id WHERE videos.id=? AND videos.verified=?",
			databaseId, verified);
		return data.FirstOrDefault();
	}

	/// <summary>
	/// Deletes a video by it's row ID from database
	/// </summary>
	/// <param name="databaseId">Row id to remove</param>
	public static async Task DeleteVideo(int databaseId)
	{
		await _db.DeleteAsync(new Types.Video
		{
			ID = databaseId,
		});
	}

	/// <summary>
	/// Sets the verify column to True for a video
	/// </summary>
	/// <param name="databaseId">The column ID</param>
	public static async Task VerifyVideo(int databaseId)
	{
		await _db.ExecuteAsync("UPDATE videos SET verified=TRUE WHERE id=?", databaseId);
	}

	/// <summary>
	/// Gets the next session id for a course
	/// </summary>
	/// <param name="courseId">The courses.id</param>
	/// <returns>Next session id</returns>
	public static async Task<int> GetNextSessionNumber(int courseId)
	{
		return await _db.ExecuteScalarAsync<int>(
			"SELECT IFNULL(MAX(session_number), 0) + 1 FROM videos WHERE course_id=? AND verified=TRUE", courseId);
	}
}