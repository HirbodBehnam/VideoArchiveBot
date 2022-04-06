using Telegram.Bot.Types.ReplyMarkups;

namespace VideoArchiveBot.Bot;

internal static class Util
{
	/// <summary>
	/// This function will get a pretty inline button matrix with next and back button if needed
	/// </summary>
	/// <param name="pivot">Should we get the IDs after this or before this</param>
	/// <param name="id">The ID to start with. This ID is inlcusive</param>
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
	
	public static async Task<InlineKeyboardMarkup?> GetAndPaginateCourseVideos(Database.HelperTypes.Pivot pivot, int courseId, int id)
	{
		// Fetch the data from database
		const int columns = 4;
		int limit = InlineButtonUtils.GetMaxElementsInPaginatedButtons(columns);
		(var courses, bool hasBefore, bool hasNext) = await Database.Database.GetVerifiedCourseVideos(pivot, id, courseId, limit);
		// Paginate if needed
		return courses.Count == 0 ? null : InlineButtonUtils.PaginateButtons(courses, columns, hasNext, hasBefore);
	}
}