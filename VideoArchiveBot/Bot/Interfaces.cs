namespace VideoArchiveBot.Bot;

internal interface ITelegramInlineButton
{
	string GetText();
	string GetCallbackData();
}

internal interface ITelegramPaginatableInlineButton : ITelegramInlineButton
{
	string GetNextPageCallbackData();
	string GetPreviousPageCallbackData();
}