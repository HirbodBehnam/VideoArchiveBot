using Telegram.Bot.Types.ReplyMarkups;

namespace VideoArchiveBot.Bot;

internal static class InlineButtonUtils
{
	private static InlineKeyboardButton NewInlineButton(string text, string query)
	{
		return new InlineKeyboardButton(text)
		{
			CallbackData = query,
		};
	}

	/// <summary>
	/// This function will create a matrix of inline buttons from a source
	/// It will return the underlying list of list of buttons
	/// </summary>
	/// <typeparam name="T">The button type</typeparam>
	/// <param name="buttons">List of buttons</param>
	/// <param name="maxColumns">Maximum number of columns to put</param>
	/// <param name="maxRows">Maximum number of rows to put</param>
	/// <returns></returns>
	private static List<List<InlineKeyboardButton>> GenerateButtonsRaw<T>(IEnumerable<T> buttons, int maxColumns = 5,
		int maxRows = 10) where T : ITelegramInlineButton
	{
		List<List<InlineKeyboardButton>> buttonMatrix = new(maxRows)
		{
			new List<InlineKeyboardButton>(maxColumns) // Add the first row
		};
		foreach (var button in buttons)
		{
			if (buttonMatrix.Last().Count == maxColumns) // If the last row is full add another one
			{
				if (buttonMatrix.Count == maxRows) // We can't add anything else
					break;
				buttonMatrix.Add(new List<InlineKeyboardButton>(maxColumns));
			}

			buttonMatrix.Last()
				.Add(NewInlineButton(button.GetText(), button.GetCallbackData())); // Always add to last row
		}

		return buttonMatrix;
	}

	/// <summary>
	/// This function will create a matrix of inline buttons from a source
	/// It will return the underlying list of list of buttons
	/// </summary>
	/// <typeparam name="T">The button type</typeparam>
	/// <param name="buttons">List of buttons</param>
	/// <param name="maxColumns">Maximum number of columns to put</param>
	/// <param name="maxRows">Maximum number of rows to put</param>
	/// <returns></returns>
	public static InlineKeyboardMarkup GenerateButtons<T>(IEnumerable<T> buttons, int maxColumns = 5, int maxRows = 10)
		where T : ITelegramInlineButton
	{
		return new InlineKeyboardMarkup(GenerateButtonsRaw(buttons, maxColumns, maxRows));
	}

	private const int PaginateButtonsMaxRows = 9;
	public static int GetMaxElementsInPaginatedButtons(int columnCount) => columnCount * PaginateButtonsMaxRows;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="buttons"></param>
	/// <param name="maxColumns"></param>
	/// <param name="hasNext"></param>
	/// <param name="hasBefore"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static InlineKeyboardMarkup PaginateButtons<T>(List<T> buttons, int maxColumns, bool hasNext, bool hasBefore)
		where T : ITelegramPaginatableInlineButton
	{
		var matrix = GenerateButtonsRaw(buttons, maxColumns, PaginateButtonsMaxRows);
		var navigationRow = new List<InlineKeyboardButton>(2);
		if (hasBefore)
			navigationRow.Add(NewInlineButton("← back", buttons[0].GetPreviousPageCallbackData()));
		if (hasNext)
			navigationRow.Add(NewInlineButton("Next →", buttons.Last().GetNextPageCallbackData()));
		if (navigationRow.Count > 0)
			matrix.Add(navigationRow);
		return new InlineKeyboardMarkup(matrix);
	}

	/// <summary>
	/// This function will create two buttons for course info message
	/// </summary>
	/// <param name="courseId">The database ID of this course</param>
	/// <returns>Buttons</returns>
	public static InlineKeyboardMarkup GenerateCourseButtons(int courseId)
	{
		return new InlineKeyboardMarkup(new InlineKeyboardButton[]
		{
			new("Get Videos")
			{
				CallbackData = Callback.GetVideosPrefix + courseId
			},
			new("Upload Video")
			{
				CallbackData = Callback.UploadVideoPrefix + courseId
			},
		});
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="databaseId"></param>
	/// <returns></returns>
	public static InlineKeyboardMarkup GenerateVideoReviewButtons(int databaseId)
	{
		return new InlineKeyboardMarkup(new InlineKeyboardButton[]
		{
			new("Accept ✅")
			{
				CallbackData = Callback.ReviewResultPrefix + "y" + databaseId
			},
			new("Reject ❌")
			{
				CallbackData = Callback.ReviewResultPrefix + "n" + databaseId
			},
		});
	}
}