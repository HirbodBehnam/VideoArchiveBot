namespace VideoArchiveBot.Database;

internal static class HelperTypes
{
	public enum Pivot
	{
		Up,
		Down,
	}

	public enum UploaderPrivacy
	{
		All,
		NameOnly,
		None
	}

	public static UploaderPrivacy UploaderPrivacyFromString(string? input)
	{
		return input switch
		{
			"name" => UploaderPrivacy.NameOnly,
			"none" => UploaderPrivacy.None,
			_ => UploaderPrivacy.All
		};
	}
}