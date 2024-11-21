namespace UpdateDirectory.Model;

public class FileHashInfo(string relativePath, DateTime modified, long length) {
	public string RelativePath { get; set; } = relativePath;
	public DateTime Modified { get; set; } = modified;
	public long Length { get; set; } = length;

	public ulong Hash { get; set; }
}
