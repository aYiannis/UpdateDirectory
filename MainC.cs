// Author: Jon
// Creation date: 9/14/2023 8:52:10 PM

using UpdateDirectory.Ancillary;
using UpdateDirectory.Model;
using Microsoft.VisualBasic.FileIO;

using System.Diagnostics;
using System.Text;

using static UpdateDirectory.Ancillary.Extensions.ConsoleEx;

long startTime = Stopwatch.GetTimestamp();

Console.OutputEncoding = Encoding.UTF8;
Console.Title = "Update Directory";

#if DEBUG
args = [
	@"H:\Games\Steam\steamapps\common\Total War WARHAMMER III",
	@"G:\Total War WARHAMMER III",
	"--missing:report",
	"--deep:false",
	"--exclude:used_mods.txt,lua_mod_log.txt",
	"--simmulate:false"
];
#endif

ParseArgs(args);

int i = 1;
FileHasher.OnStartProcessingNewFile = fp => WriteLine($"Processing ({i++}): {fp}");

var (sdir, ddir) = args; // deconstruct array
if (!Directory.Exists(sdir)) throw new Exception("Invalid source directory given.\n\t'"+sdir+"'");

// This is a very heavy operation it will take time...
var results = await FileHasher.Compare(sdir, ddir);

foreach (var (hi,compareResult) in results) {
	if (compareResult == FileCompareResult.Match) continue;

	string spath = Path.Combine(sdir, hi.RelativePath);
	string dpath = Path.Combine(ddir, hi.RelativePath);

	switch(compareResult) {
		default: throw new Exception("Unexpected / invalid file comparison result.");
		case FileCompareResult.New:
			// New file in the source that doesn't exist in the destination.
			WriteEmphatic("+ New file: '", hi.RelativePath, "'. Copying ", Humanizer.Size(hi.Length), ".");
			// make sure that the destination directory exists
			string dirPath = Path.GetDirectoryName(dpath)!;
			if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
			// and copy the file from the source to the destination
			if (!Simulate) File.Copy(spath, dpath);
			break;
		case FileCompareResult.Missing:
			// The file in the destination is not in the source.
			if (Missing == Handle.Ignore) break;

			WriteEmphatic("- File '", hi.RelativePath, "' is missing from the source.");
			if (Missing == Handle.Report) break;

			// ask the user for input
			if (!Simulate && Ask("Do you want to delete it?")) {
				// recycle the file
				FileSystem.DeleteFile(dpath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
			}
			break;
		case FileCompareResult.Unmatch:
			// The files do not match, needs to be replaced
			WriteEmphatic("• Replacing file ", hi.RelativePath, " (", Humanizer.Size(hi.Length), ")...");
			if (!Simulate) FileSystem.DeleteFile(dpath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
			if (!Simulate) File.Copy(spath, dpath);
			break;
	}
}

WriteLine($"Elapsed: {TimeSpan.FromTicks(Stopwatch.GetTimestamp() - startTime).TotalMilliseconds} ms.");
Console.ReadLine();
