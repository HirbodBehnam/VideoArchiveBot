using SQLite;
using VideoArchiveBot.Bot;

namespace VideoArchiveBot.Database;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
internal static class Types
{
	private static readonly HelperTypes.UploaderPrivacy PrivacySettings =
		HelperTypes.UploaderPrivacyFromString(Environment.GetEnvironmentVariable("UPLOADER_PRIVACY"));

	[Table("courses")]
	public class Course
	{
		[Column("id"), PrimaryKey, AutoIncrement]
		public int ID { get; set; }

		[Column("name")] public string Name { get; set; }
		[Column("lecturer")] public string Lecturer { get; set; }
		[Column("group_id")] public int GroupID { get; set; }
		[Column("course_id")] public int CourseID { get; set; }

		public override string ToString()
		{
			return $"{Name}\nLecturer: {Lecturer}\nID: {CourseID}-{GroupID}";
		}
	}

	[Table("videos")]
	public class Video
	{
		[Column("id"), PrimaryKey, AutoIncrement]
		public int ID { get; set; }

		/// <summary>
		/// Foreign key to <see cref="Course.ID"/>
		/// </summary>
		[Column("course_id")]
		public int CourseID { get; set; }

		[Column("video_file_id")] public string VideoFileID { get; set; }
		[Column("session_number")] public int SessionNumber { get; set; }

		/// <summary>
		/// Foreign key to <see cref="Users.UserID"/>
		/// </summary>
		[Column("uploader")]
		public long Uploader { get; set; }

		[Column("topic")] public string? Topic { get; set; }
		[Column("session_date")] public DateTime SessionDate { get; set; }
		[Column("added_time")] public DateTime AddedTime { get; set; }
		[Column("verified")] public bool Verified { get; set; }
	}

	[Table("users")]
	public class Users
	{
		/// <summary>
		/// Telegram user ID
		/// </summary>
		[Column("user_id")]
		public long UserID { get; set; }

		[Column("username")] public string? Username { get; set; }
		[Column("name")] public string Name { get; set; }
		[Column("is_admin")] public bool IsAdmin { get; set; }
	}

	public class CourseBasicInfo : ITelegramPaginatableInlineButton
	{
		/// <summary>
		/// The ID in the database. Not the course ID
		/// </summary>
		[Column("id")]
		public int ID { get; set; }

		[Column("name")] public string Name { get; set; }
		[Column("group_id")] public int GroupID { get; set; }


		public string GetCallbackData()
		{
			return Callback.GetCoursePrefix + ID;
		}

		public string GetText()
		{
			return Name + " G" + GroupID;
		}

		public string GetNextPageCallbackData()
		{
			return Callback.GetCoursePageAfterPrefix + ID;
		}

		public string GetPreviousPageCallbackData()
		{
			return Callback.GetCoursePageBeforePrefix + ID;
		}
	}

	public class VideoSessionInfo : ITelegramPaginatableInlineButton
	{
		/// <summary>
		/// The video ID in the database. Not the course ID
		/// </summary>
		[Column("id")]
		public int Id { get; set; }

		[Column("session_number")] public int SessionNumber { get; set; }

		[Column("course")] public int CourseId { get; set; }

		public string GetCallbackData()
		{
			return Callback.GetVideoPrefix + Id;
		}

		public string GetText()
		{
			return "Session " + SessionNumber;
		}

		public string GetNextPageCallbackData()
		{
			return Callback.GetVideosPageAfterPrefix + SessionNumber + "_" + CourseId;
		}

		public string GetPreviousPageCallbackData()
		{
			return Callback.GetVideosPageBeforePrefix + SessionNumber + "_" + CourseId;
		}
	}

	public class GetVideoResult
	{
		[Column("video_id")] public int VideoId { get; set; }
		[Column("video_file_id")] public string VideoFileID { get; set; }
		[Column("topic")] public string? Topic { get; set; }
		[Column("session_number")] public int SessionNumber { get; set; }
		[Column("session_date")] public DateTime SessionDate { get; set; }
		[Column("added_time")] public DateTime AddedTime { get; set; }
		[Column("name")] public string UploaderName { get; set; }
		[Column("username")] public string? UploaderUsername { get; set; }
		[Column("course")] public string CourseName { get; set; }

		private string UsernameData => UploaderUsername == null ? string.Empty : "(@" + UploaderUsername + ")";

		public string ToReviewString()
		{
			return
				$"{CourseName}\nS{SessionNumber} at {DateOnly.FromDateTime(SessionDate)}\nFrom: {UploaderName} {UsernameData}\n/{Commands.ReviewVideoPrefix}{VideoId}\n";
		}

		public override string ToString()
		{
			return ToString(false);
		}

		public string ToString(bool isAdmin)
		{
			string result = $"{CourseName}\nSession {SessionNumber} at {DateOnly.FromDateTime(SessionDate)}\n";
			if (isAdmin || PrivacySettings == HelperTypes.UploaderPrivacy.All)
				result += $"From: {UploaderName} {UsernameData}\n";
			else if (PrivacySettings == HelperTypes.UploaderPrivacy.NameOnly)
				result += $"From: {UploaderName}\n";
			result += $"Uploaded at {AddedTime}\nTopic: {Topic ?? "-"}";
			return result;
		}
	}
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.