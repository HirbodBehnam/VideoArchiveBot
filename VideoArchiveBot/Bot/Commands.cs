using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;

namespace VideoArchiveBot.Bot;

internal static class Commands
{
	public const string ReviewVideoPrefix = "review_";

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
				// TODO: Fix
				await bot.SendTextMessageAsync(message.Chat.Id,
					"Welcome to bot! Use /help to get the list of commands.");
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
				await SendVideoForReview(bot, message.Chat.Id, int.Parse(s[(ReviewVideoPrefix.Length + 1)..]));
				break;
			case "/review":
				if (await Database.Database.IsAdmin(message.From!) == false)
					return false;
				string data = await Database.Database.GetUnverifiedVideosList();
				if (data.Length == 0)
					data = "Nothing to review!";
				await bot.SendTextMessageAsync(message.Chat.Id, data);
				break;
			default:
				return false;
		}

		return true;
	}

	private static async Task SendCoursesList(ITelegramBotClient bot, ChatId chatId)
	{
		var buttons = await Util.GetAndPaginateCourses(Database.HelperTypes.Pivot.Up, 1);
		// Create the buttons and send the message
		await bot.SendTextMessageAsync(chatId,
			"Please select a course:",
			replyMarkup: buttons);
	}

	private static async Task SendVideoForReview(ITelegramBotClient bot, ChatId chatId, int databaseId)
	{
		var video = await Database.Database.GetVideo(databaseId);
		await bot.SendVideoAsync(chatId, new InputOnlineFile(video.VideoFileID), caption: video.ToString(),
			replyMarkup: InlineButtonUtils.GenerateVideoReviewButtons(databaseId));
	}
}