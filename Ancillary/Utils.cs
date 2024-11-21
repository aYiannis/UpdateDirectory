namespace UpdateDirectory.Ancillary;
internal static class Utils {
	internal static void ParseArgs(string[] args) {
		for (int i = Math.Max(args.Length - 1, 2); i >= 2; i--) {
			ReadOnlySpan<char> arg = args[i].AsSpan();
			if (!arg.StartsWith("--")) continue;

			int indexOfColumn = arg.IndexOf(':');
			switch (arg[2..(indexOfColumn > -1 ? indexOfColumn : ^0)]) {
				case Keywords.Missing:
					Missing = arg[(Keywords.Missing.Length + 3)..] switch {
						"report" => Handle.Report,
						"ignore" => Handle.Ignore,
						"ask" => Handle.Ask,
						_ => throw new Exception("Invalid handle specified!")
					};
					break;
				case Keywords.SectionSize:
					SectionSize = 10;
					break;
				case Keywords.Deep: // default false
					Deep = indexOfColumn != -1 && bool.Parse(arg[(indexOfColumn + 1)..]);
					break;
				case Keywords.Exclude:
					Excluded = args[i][(Keywords.Exclude.Length + 3)..].Split(',')
						.Select(relativePath => relativePath.Replace('/', '\\'))
						.ToHashSet();
					break;
				case Keywords.Simulate: // default false
					Simulate = indexOfColumn != -1 && bool.Parse(arg[(indexOfColumn + 1)..]);
					break;
			}
		}
	}

	static class Keywords {
		internal const string Simulate = "simmulate";
		internal const string Deep = "deep";
		internal const string Missing = "missing";
		internal const string Exclude = "exclude";
		internal const string SectionSize = "section-size";
	}
}
