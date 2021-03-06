using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace VideoArchiveBot.Bot;

internal static class Util
{
	/// <summary>
	/// This function will get a pretty inline button matrix with next and back button if needed
	/// </summary>
	/// <param name="pivot">Should we get the IDs after this or before this</param>
	/// <param name="id">The ID to start with. This ID is inclusive</param>
	/// <returns></returns>
	public static async Task<InlineKeyboardMarkup> GetAndPaginateCourses(Database.HelperTypes.Pivot pivot, int id)
	{
		// Fetch the data from database
		const int columns = 2;
		int limit = InlineButtonUtils.GetMaxElementsInPaginatedButtons(columns);
		(var courses, bool hasBefore, bool hasNext) = await Database.Database.GetCoursesNames(pivot, id, limit);
		// Paginate
		return InlineButtonUtils.PaginateButtons(courses, columns, hasNext, hasBefore);
	}
	
	/// <summary>
	/// Basically something like <see cref="GetAndPaginateCourses"/> but for course videos
	/// </summary>
	/// <param name="pivot">Should we go up or down the rows?</param>
	/// <param name="courseId">The course id</param>
	/// <param name="sessionNumber">The session number which <see cref="pivot"/> is for</param>
	/// <returns>A list of buttons which can give videos to user. Or null if nothing exists</returns>
	public static async Task<InlineKeyboardMarkup?> GetAndPaginateCourseVideos(Database.HelperTypes.Pivot pivot, int courseId, int sessionNumber)
	{
		// Fetch the data from database
		const int columns = 4;
		int limit = InlineButtonUtils.GetMaxElementsInPaginatedButtons(columns);
		(var courses, bool hasBefore, bool hasNext) = await Database.Database.GetVerifiedCourseVideos(pivot, sessionNumber, courseId, limit);
		// Paginate if needed
		return courses.Count == 0 ? null : InlineButtonUtils.PaginateButtons(courses, columns, hasNext, hasBefore);
	}
	
	public static async Task SendCourseVideo(ITelegramBotClient bot, ChatId chatId, int videoId)
	{
		var video = await Database.Database.GetVideo(videoId);
		if (video == null)
		{
			await bot.SendTextMessageAsync(chatId, "Video not found!");
			return;
		}
		await bot.SendVideoAsync(chatId, new InputOnlineFile(video.VideoFileID),
			caption: video.ToString());
	}
}