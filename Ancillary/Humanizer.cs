namespace UpdateDirectory.Ancillary;
internal class Humanizer {
	readonly static string[] _units = ["", "K", "M", "G", "T", "P", "E"];
	readonly static int _unitsCount = _units.Length-1;
	/// <summary>
	/// Converts the long to a human-readable string about the file's size.
	/// <example>Size(1024) => 1KB</example>
	/// </summary>
	/// <param name="size">The size in bytes.</param>
	public static string Size(long size) {
		const double STEP = 1024.0;
		bool isNegative = size < 0;
		double currentSize = isNegative ? -size : size;
		int unitIndex = 0;
		while (currentSize >= STEP && unitIndex < _unitsCount)
			currentSize /= STEP;
		return (isNegative ? "-" : "") + $"{currentSize:n} {_units[unitIndex]}B";
	}
}
