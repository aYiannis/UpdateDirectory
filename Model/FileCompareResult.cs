namespace UpdateDirectory.Model;

public enum FileCompareResult {
	// the files have not been checked yet
	Undefined,
	// the files have identical hashes
	Match,
	// the files have unequal hashes
	Unmatch,
	// the file in source is not in destination
	New,
	// the file in destination is not in source
	Missing
};