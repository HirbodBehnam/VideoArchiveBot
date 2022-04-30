using System.IO.Compression;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using File = System.IO.File;

namespace VideoArchiveBot.Bot;

internal static class Commands
{
	public const string ReviewVideoPrefix = "review_";
	public const string GetVideoPrefix = "get_video_";

	public static async Task<bool> CheckAndHandleCommand(ITelegramBotClient bot, Message message)
	{
		if (message.Text == null)
			return false;
		switch (message.Text)
		{
			case "/start":
				await bot.SendTextMessageAsync(message.Chat.Id,
					"Welcome to bot! Use /help to get the list of commands.");
				break;
			case "/help":
				await bot.SendTextMessageAsync(message.Chat.Id,
					await GenerateHelp(message.From!.Id));
				break;
			case "/courses":
				await SendCoursesList(bot, message.Chat.Id);
				break;
			case "/cancel":
				Bot.UsersState.Remove(message.From!.Id); // remove from states
				await bot.SendTextMessageAsync(message.Chat.Id, "Canceled!",
					replyMarkup: new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove());
				break;
			case { } s when s.StartsWith("/" + ReviewVideoPrefix):
				// Non admins cannot review!
				if (await Database.Database.IsAdmin(message.From!) == false)
					return false;
				// Send the video
				await SendVideoForReview(bot, message.Chat.Id, int.Parse(s[(ReviewVideoPrefix.Length + 1)..]));
				break;
			case { } s when s.StartsWith("/" + GetVideoPrefix):
				// Send the video
				await Util.SendCourseVideo(bot, message.Chat.Id, int.Parse(s[(GetVideoPrefix.Length + 1)..]));
				break;
			case "/review":
				// Non admins cannot review!
				if (await Database.Database.IsAdmin(message.From!) == false)
					return false;
				// Send the list
				string data = await Database.Database.GetUnverifiedVideosList();
				if (data.Length == 0)
					data = "Nothing to review!";
				await bot.SendTextMessageAsync(message.Chat.Id, data);
				break;
			case "/db_dump":
				// Non admins get the database!
				if (await Database.Database.IsAdmin(message.From!) == false)
					return false;
				await UploadDatabase(bot, message.Chat.Id);
				break;
			default:
				return false;
		}

		return true;
	}

	/// <summary>
	/// This function will send courses list to user with inline buttons to choose the course from
	/// </summary>
	/// <param name="bot">The bot</param>
	/// <param name="chatId">Chat id of user</param>
	private static async Task SendCoursesList(ITelegramBotClient bot, ChatId chatId)
	{
		var buttons = await Util.GetAndPaginateCourses(Database.HelperTypes.Pivot.Up, 1);
		// Create the buttons and send the message
		await bot.SendTextMessageAsync(chatId,
			"Please select a course:",
			replyMarkup: buttons);
	}

	/// <summary>
	/// This method will send the video to one admin in order to review it
	/// </summary>
	/// <param name="bot">Bot</param>
	/// <param name="chatId">The admin chat id</param>
	/// <param name="databaseId">The video row id in database</param>
	private static async Task SendVideoForReview(ITelegramBotClient bot, ChatId chatId, int databaseId)
	{
		var video = await Database.Database.GetVideo(databaseId, false);
		if (video == null)
		{
			await bot.SendTextMessageAsync(chatId, "Video not found!");
			return;
		}
		await bot.SendVideoAsync(chatId, new InputOnlineFile(video.VideoFileID), caption: video.ToString(true),
			replyMarkup: InlineButtonUtils.GenerateVideoReviewButtons(databaseId));
	}

	/// <summary>
	/// This method will generate help based on if user is admin or not
	/// </summary>
	/// <param name="userId">The user's ID</param>
	/// <returns>The help message</returns>
	private static async Task<string> GenerateHelp(long userId)
	{
		string help = "For starting point use /courses command to get the list of courses. From there you can " +
		              "use the inline buttons to navigate through courses and videos.\nYou can also upload your own video! " +
		              "To do so, just select the course you want to upload the video, then click on upload video. From there " +
		              "you can upload your video upto one hour after it to the bot. (you can also forward the video.)\n" +
		              "At anytime, you can cancel the process using /cancel";
		if (await Database.Database.IsAdmin(userId))
			help +=
				"\nYou are admin so you can use /review to review unverified videos.\nYou can also use /db_dump to get " +
				"a backup from bot's database.";
		return help;
	}

	private static async Task UploadDatabase(ITelegramBotClient bot, ChatId chatId)
	{
		// Create temp location to copy the file into it
		string dbTempLocation = Path.GetTempFileName();
		// Create a memory stream to send the file into it
		var gzipPipe = new System.IO.Pipelines.Pipe();
		try
		{
			// Copy the file in order to bypass file locks
			File.Copy(VideoArchiveBot.Util.ProgramUtil.DatabasePath, dbTempLocation, true);
			// Create the compress stream and the file stream in a new task
			new Task(async () =>
			{
				await using var compressStream =
					new GZipStream(gzipPipe.Writer.AsStream(), CompressionMode.Compress, false);
				await using var file = new FileStream(dbTempLocation, FileMode.Open, FileAccess.Read, FileShare.Read);
				await file.CopyToAsync(compressStream);
			}).Start();
			// Also simultaneously send the file to telegram servers
			await bot.SendDocumentAsync(chatId, new InputMedia(gzipPipe.Reader.AsStream(), "database.db.gz"));
		}
		finally
		{
			File.Delete(dbTempLocation);
			await gzipPipe.Writer.CompleteAsync();
			await gzipPipe.Reader.CompleteAsync();
		}
	}
}