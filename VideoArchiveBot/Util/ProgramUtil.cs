namespace VideoArchiveBot.Util;

public static class ProgramUtil
{
	public static string DatabasePath => Environment.GetEnvironmentVariable("DB_PATH") ?? "database.db";
}