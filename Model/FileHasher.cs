using K4os.Hash.xxHash;

using System.Buffers;

using CompareResultPair = (UpdateDirectory.Model.FileHashInfo HashInfo, UpdateDirectory.Model.FileCompareResult);

namespace UpdateDirectory.Model;

public class FileHasher {
	public const int OPTIMAL_BUFFER_SIZE = 4 * 1024 * 1024; // 4MB

	static Action<string>? onStartProcessingNewFile;
	public static Action<string>? OnStartProcessingNewFile {
		get => onStartProcessingNewFile;
		set => onStartProcessingNewFile = value;
	}

	public static async Task<Dictionary<string, FileHashInfo>> ComputeDirectoryHashes(string directoryPath, bool deep = true) {
		// normalize the directory path
		directoryPath = Path.GetFullPath(directoryPath);

		// try to get the last stored info for the directory
		Dictionary<string, FileHashInfo> hashes = [];
		if (Directories.TryGetValue(directoryPath.ToLowerInvariant(), out var dirHashes) && dirHashes is not null)
			hashes = dirHashes.ToDictionary(f => f.RelativePath.ToLowerInvariant());

		var directoryInfo = new DirectoryInfo(directoryPath);
		foreach (var fi in directoryInfo.EnumerateFiles("*", deep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)) {
			string relativePath = fi.FullName[(directoryPath.Length + 1)..];
			string pathKey = relativePath.ToLowerInvariant();

			if (Excluded?.Contains(pathKey) ?? false) continue;

			if (!hashes.TryGetValue(pathKey, out var hash))
				hashes[pathKey] = new FileHashInfo(relativePath, fi.LastWriteTimeUtc, fi.Length);
			else if (hash.Modified != fi.LastWriteTimeUtc || hash.Length != fi.Length) {
				hash.Modified = fi.LastWriteTime;
				hash.Length = fi.Length;
				hashes[pathKey] = new FileHashInfo(relativePath, fi.LastWriteTimeUtc, fi.Length);
			}
		}

		// calculate the actual hashes of the files that require update
		var requiringUpdate = hashes.Where(p => p.Value.Hash == default)
			.Select(p => Path.Combine(directoryPath, p.Value.RelativePath))
			.ToArray();
		// no calculations required, all hashes are up to date
		if (requiringUpdate.Length == 0)
			return hashes;
		ulong[] hashesArray = await ComputeHashesAsync(requiringUpdate);
		int index = 0;
		foreach (var (rp, fh) in hashes) {
			if (fh.Hash == default)
				fh.Hash = hashesArray[index++];
		}

		// store the results
		Directories.Set(directoryPath.ToLowerInvariant(), [.. hashes.Values]);

		return hashes;
	}

	public static async Task<ulong[]> ComputeHashesAsync(string[] filePaths, int bufferSize = OPTIMAL_BUFFER_SIZE) {
		if (filePaths.Length == 0) return [];

		byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

		var hasher = new XXH64();

		int length = filePaths.Length;
		ulong[] results = new ulong[length];

		try {
			for (int i = 0; i < length; i++) {
				string filePath = filePaths[i];
				hasher.Reset();

				OnStartProcessingNewFile?.Invoke(filePath);
				using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

				int bytesRead;
				while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
					hasher.Update(buffer, 0, bytesRead);

				results[i] = hasher.Digest();
			}
		} finally {
			ArrayPool<byte>.Shared.Return(buffer);
		}
		return results;
	}

	public static async Task<List<CompareResultPair>> Compare(string sdir, string ddir, bool deep = false) {
		var source = await ComputeDirectoryHashes(sdir, deep);
		var destination = await ComputeDirectoryHashes(ddir, deep);

		var results = new List<CompareResultPair>(source.Count);

		// Check for files in source that are not in destination
		foreach (var (k, v) in source) {
			if (!destination.TryGetValue(k, out var fh)) {
				results.Add((v, FileCompareResult.New));
			} else {
				results.Add((v, fh.Hash == v.Hash ? FileCompareResult.Match : FileCompareResult.Unmatch));
			}
		}

		// Check for files in destination that are not in source
		foreach (var (k, v) in destination) {
			if (!source.TryGetValue(k, out var _)) {
				results.Add((v, FileCompareResult.Missing));
			}
		}

		return results;
	}
}
