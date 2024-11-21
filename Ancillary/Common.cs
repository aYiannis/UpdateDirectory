#region Global usings
global using UpdateDirectory.Ancillary.Extensions;

global using System;
global using System.Collections.Generic;

global using static UpdateDirectory.Ancillary.Common;
global using static UpdateDirectory.Ancillary.Utils;
#endregion

using UpdateDirectory.Model;

namespace UpdateDirectory.Ancillary;

public static partial class Common {
	public static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

	public static MapDir<FileHashInfo[]> Directories { get; set; } = new(".dirs");
	
	#region CLI-Params
	internal static Handle Missing { get; set; } = Handle.Report;
	internal static bool Deep { get; set; } = false;
	internal static int SectionSize { get; set; } = 104857600; // 10MB
	internal static HashSet<string>? Excluded { get; set; }
	internal static bool Simulate { get; set; } = false;
	#endregion

	public enum Handle {
		Ask,
		Report,
		Ignore,
	};
}
