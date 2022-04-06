using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using VideoArchiveBot.Database;

namespace VideoArchiveBot.Bot;

internal static class Bot
{
	public static readonly UsersState UsersState = new();

	public static async Task StartBot(string token)
	{
		// Setup the bot
		TelegramBotClient bot = new(token);
		string? username = (await bot.GetMeAsync()).Username;
		Console.WriteLine("Authorized on " + username);
		using var cts = new CancellationTokenSource();
		// Listen for updates
		ReceiverOptions receiverOptions = new();
		bot.StartReceiving(HandleUpdateAsync,
			HandleErrorAsync,
			receiverOptions,
			cts.Token);
		// Wait for Ctrl + C
		var cancelBot = new VideoArchiveBot.Util.AsyncBlocker();
		Console.CancelKeyPress += delegate { cancelBot.Continue(); };
		await cancelBot;
		cts.Cancel();
	}

	private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
		CancellationToken cancellationToken)
	{
		string errorMessage = exception switch
		{
			ApiRequestException apiRequestException =>
				$"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
			_ => exception.ToString()
		};

		Console.WriteLine(errorMessage);
		return Task.CompletedTask;
	}

	private static Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
		CancellationToken cancellationToken)
	{
		new Task(async () =>
		{
			var handler = update.Type switch
			{
				UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
				UpdateType.CallbackQuery => BotOnCallbackQueryReceived(botClient, update.CallbackQuery!),
				_ => Task.CompletedTask,
			};
			try
			{
				await handler;
			}
			catch (Exception exception)
			{
				await HandleErrorAsync(botClient, exception, cancellationToken);
			}
		}).Start();

		return Task.CompletedTask;
	}

	private static async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
	{
		// The fuck?
		if (message.From == null)
			return;
		await Database.Database.RegisterUser(message.From);
		// Check command
		if (await Commands.CheckAndHandleCommand(botClient, message))
			return;
		// Check for video
		if (message.Video != null)
		{
			const string invalidState = "Please select a /course at first to send the video to";
			try
			{
				UsersState.SetVideo(message.From.Id, message.Video.FileId);
				await botClient.SendTextMessageAsync(message.From.Id, "Now send the session number");
			}
			catch (KeyNotFoundException)
			{
				await botClient.SendTextMessageAsync(message.From.Id, invalidState);
			}
			catch (InvalidOperationException)
			{
				await botClient.SendTextMessageAsync(message.From.Id, invalidState);
			}

			return;
		}

		// Get the user state
		if (message.Text == null)
			return;
		try
		{
			const string emptyTopic = "-";
			const string sendTodayDate = "Today";
			switch (UsersState.GetUsersState(message.From.Id))
			{
				case UsersState.UserState.SessionNumber:
					// Parse the session number
					if (!int.TryParse(message.Text, out int sessionNumber))
						throw new FormatException("cannot parse the session number!");
					// Update state
					UsersState.SetSessionNumber(message.From.Id, sessionNumber);
					// Inform user of next move
					await botClient.SendTextMessageAsync(message.From.Id,
						"Now send the session date in format of YYYY-MM-DD",
						replyMarkup: new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(
							new Telegram.Bot.Types.ReplyMarkups.KeyboardButton(sendTodayDate))
						{
							OneTimeKeyboard = true
						});
					break;
				case UsersState.UserState.SessionDate:
					DateOnly date;
					if (message.Text == sendTodayDate) // Just set it to today
						date = DateOnly.FromDateTime(DateTime.Now);
					else if (!DateOnly.TryParseExact(message.Text, "yyyy-MM-dd", out date))
						throw new FormatException("cannot parse the date!");
					// Update state
					UsersState.SetSessionDate(message.From.Id, date);
					// Inform user of next move
					await botClient.SendTextMessageAsync(message.From.Id,
						"Now send the session topic. If there is no topic, send \"-\".",
						replyMarkup: new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(
							new Telegram.Bot.Types.ReplyMarkups.KeyboardButton(emptyTopic))
						{
							OneTimeKeyboard = true
						});
					break;
				case UsersState.UserState.Topic:
					// Set the topic
					string? topic = message.Text == emptyTopic ? null : message.Text;
					UsersState.SetSessionTopic(message.From.Id, topic);
					// Store in database
					var videoInDatabase = UsersState.GetDatabaseTypeFromData(message.From.Id);
					await Database.Database.AddVideo(videoInDatabase);
					// Done I guess?
					await botClient.SendTextMessageAsync(message.From.Id,
						"Done! Your video has been submitted for review...",
						replyMarkup: new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove());
					break;
			}
		}
		catch (KeyNotFoundException)
		{
			// do nothing...
		}
		catch (FormatException ex)
		{
			await botClient.SendTextMessageAsync(message.From.Id, ex.Message);
		}
	}

	private static async Task BotOnCallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
		if (callbackQuery.Data == null)
			return;
		switch (callbackQuery.Data)
		{
			case { } s when s.StartsWith(Callback.GetCoursePageAfterPrefix):
				await Callback.UpdateCoursesPage(botClient, callbackQuery,
					HelperTypes.Pivot.Up,
					int.Parse(s[Callback.GetCoursePageAfterPrefix.Length..]) + 1);
				break;
			case { } s when s.StartsWith(Callback.GetCoursePageBeforePrefix):
				await Callback.UpdateCoursesPage(botClient, callbackQuery,
					HelperTypes.Pivot.Down,
					int.Parse(s[Callback.GetCoursePageAfterPrefix.Length..]) - 1);
				break;
			case { } s when s.StartsWith(Callback.GetCoursePrefix):
				await Callback.ShowCourse(botClient, callbackQuery,
					int.Parse(s[Callback.GetCoursePrefix.Length..]));
				break;
			case { } s when s.StartsWith(Callback.UploadVideoPrefix):
				// Register in users state
				UsersState.AddStateForUser(int.Parse(s[Callback.UploadVideoPrefix.Length..]), callbackQuery.From.Id);
				await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
				// Send the information message
				await botClient.SendTextMessageAsync(callbackQuery.From.Id, "Please send the video to bot");
				break;
			case { } s when s.StartsWith(Callback.ReviewResultPrefix):
			{
				// Check admin
				if (await Database.Database.IsAdmin(callbackQuery.From.Id) == false)
					break;
				// Is this accepted?
				bool accepted = s[Callback.ReviewResultPrefix.Length] == 'y';
				// Get video ID
				int videoId = int.Parse(s[(Callback.ReviewResultPrefix.Length + 1)..]);
				if (accepted)
					await Database.Database.VerifyVideo(videoId);
				else
					await Database.Database.DeleteVideo(videoId);
				// Now inform the user
				await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Done");
				if (callbackQuery.Message != null)
					await botClient.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);
			}
				break;
			case { } s when s.StartsWith(Callback.GetVideosPageAfterPrefix):
			{
				int[] payload = s[Callback.GetVideosPageAfterPrefix.Length..].Split('_').Select(int.Parse).ToArray();
				await Callback.UpdateVideoSessionsPage(botClient, callbackQuery, HelperTypes.Pivot.Up, payload[1],
					payload[0]);
			}
				break;
			case { } s when s.StartsWith(Callback.GetVideosPageBeforePrefix):
			{
				int[] payload = s[Callback.GetVideosPageAfterPrefix.Length..].Split('_').Select(int.Parse).ToArray();
				await Callback.UpdateVideoSessionsPage(botClient, callbackQuery, HelperTypes.Pivot.Down, payload[1],
					payload[0]);
			}
				break;
			case { } s when s.StartsWith(Callback.GetVideosPrefix):
				await Callback.SendCourseVideos(botClient, callbackQuery,
					int.Parse(s[Callback.GetVideosPrefix.Length..]));
				break;
			case { } s when s.StartsWith(Callback.GetVideoPrefix):
				await Callback.SendCourseVideo(botClient, callbackQuery,
					int.Parse(s[Callback.GetVideoPrefix.Length..]));
				break;
		}
	}
}