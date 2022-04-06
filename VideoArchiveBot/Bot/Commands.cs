﻿using Telegram.Bot;
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
		var video = await Database.Database.GetVideo(databaseId);
		await bot.SendVideoAsync(chatId, new InputOnlineFile(video.VideoFileID), caption: video.ToString(),
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
			help += "\nYou are admin so you can use /review to review unverified videos";
		return help;
	}
}