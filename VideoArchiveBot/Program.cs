namespace VideoArchiveBot;

internal class Program
{
	static async Task Main()
	{
		// Open database connection
		await Database.Database.OpenConnection(
			Environment.GetEnvironmentVariable("DB_PATH") ?? "database.db"
			);
		// Start bot
		string? token = Environment.GetEnvironmentVariable("BOT_TOKEN");
		if (token == null)
		{
			Console.WriteLine("Please set the BOT_TOKEN as environment variable");
			return;
		}
		await Bot.Bot.StartBot(token);
		await Database.Database.CloseDatabase();
		Console.WriteLine("Clean shutdown");
	}
}