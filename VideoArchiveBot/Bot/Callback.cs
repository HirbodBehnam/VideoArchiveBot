using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using VideoArchiveBot.Database;

namespace VideoArchiveBot.Bot;

internal static class Callback
{
	public const string GetCoursePrefix = "getcourse";
	public const string GetCoursePageAfterPrefix = "getcoursepage_n";
	public const string GetCoursePageBeforePrefix = "getcoursepage_p";
	public const string GetVideoSessionsPrefix = "getsessionvideos";
	public const string GetCourseVideoTopicsPrefix = "getvideostopics";
	public const string GetVideosPageAfterPrefix = "getvideos_n";
	public const string GetVideosPageBeforePrefix = "getvideos_p";
	public const string GetVideoPrefix = "getvideo";
	public const string UploadVideoPrefix = "uploadvideo";
	public const string ReviewResultPrefix = "review";

	/// <summary>
	/// This method will update courses pages of a message
	/// Update means going to next page or previous page
	/// </summary>
	/// <param name="bot">Bot</param>
	/// <param name="callbackQuery">The callback query of database</param>
	/// <param name="pivot">Should we go to next page or previous</param>
	/// <param name="id"></param>
	/// <returns></returns>
	public static async Task UpdateCoursesPage(ITelegramBotClient bot, CallbackQuery callbackQuery,
		HelperTypes.Pivot pivot, int id)
	{
		// Fetch data
		var buttons = await Util.GetAndPaginateCourses(pivot, id);
		// Answer the callback
		await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
		// Create the buttons and edit the message
		await bot.EditMessageReplyMarkupAsync(callbackQuery.From.Id,
			callbackQuery.Message!.MessageId,
			buttons);
	}

	/// <summary>
	/// This function will send a new message to user with course info
	/// </summary>
	/// <param name="bot">Bot</param>
	/// <param name="callbackQuery">The callback query</param>
	/// <param name="dbId"></param>
	/// <returns></returns>
	public static async Task ShowCourse(ITelegramBotClient bot, CallbackQuery callbackQuery, int dbId)
	{
		var course = await Database.Database.GetCourse(dbId);
		// Answer the callback
		await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
		// Edit the message
		await bot.SendTextMessageAsync(callbackQuery.From.Id,
			course.ToString(),
			replyMarkup: InlineButtonUtils.GenerateCourseButtons(dbId));
	}

	public static async Task SendCourseVideos(ITelegramBotClient bot, CallbackQuery callbackQuery, int courseId)
	{
		var buttons = await Util.GetAndPaginateCourseVideos(HelperTypes.Pivot.Up, courseId, 1);
		await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
		if (buttons == null)
		{
			await bot.SendTextMessageAsync(callbackQuery.From.Id,
				"No video is uploaded for this course :(");
			return;
		}

		// Create the buttons and send the message
		await bot.SendTextMessageAsync(callbackQuery.From.Id,
			"Please select a session:",
			replyMarkup: buttons);
	}

	public static async Task UpdateVideoSessionsPage(ITelegramBotClient bot, CallbackQuery callbackQuery,
		HelperTypes.Pivot pivot, int courseId, int sessionNumber)
	{
		// Fetch data
		var buttons = await Util.GetAndPaginateCourseVideos(pivot, courseId, sessionNumber);
		// Answer the callback
		await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
		// Create the buttons and edit the message
		await bot.EditMessageReplyMarkupAsync(callbackQuery.From.Id,
			callbackQuery.Message!.MessageId,
			buttons);
	}

	public static async Task SendCourseVideosTitles(ITelegramBotClient bot, CallbackQuery callbackQuery, int courseId)
	{
		// Fetch data
		var videos = await Database.Database.GetVerifiedVideoTopics(courseId);
		// Create the message
		StringBuilder messageText = new(videos.Count * 50);
		foreach (var video in videos)
			messageText.AppendLine(video.ToString());
		// Answer the callback
		await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
		// Send the message
		await bot.SendTextMessageAsync(callbackQuery.From.Id, messageText.ToString());
	}
}