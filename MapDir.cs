using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace UpdateDirectory;
// A string to T Directory to the filesystem. (no RAM cost, practically limitless size)
public class MapDir<T>:IDictionary<string, T> {
	// The root folder name
	readonly string _name;
	public string Name => _name;

	readonly string _rootPath;
	public string RootPath => _rootPath;

	readonly Lock @lock = new();

	public const string COUNT_FILE_NAME = "count";
	public const string KEYS_FILE_NAME = "keys";

	Action<T, Stream> _serializer = DefaultSerializer;
	public Action<T,Stream> Serializer {
		get => _serializer;
		set => _serializer = value;
	}

	Func<Stream, T?> _deserializer = DefaultDeserializer;
	public Func<Stream, T?> Deserializer {
		get => _deserializer;
		set => _deserializer = value;
	}

	public MapDir(string name = ".cache", string? rootPath = null) {
		_name = name;
		rootPath ??= BaseDirectory + name;
		if (!Directory.Exists(rootPath)) {
			Directory.CreateDirectory(rootPath);
			_rootPath = rootPath[^1] == '/' ? rootPath : (rootPath + '/');
			Count = 0;
		} else {
			_rootPath = rootPath[^1] == '/' ? rootPath : (rootPath + '/');
			Count = readCount();
		}
	}


	#region Static Utilities (maybe move them to another file)
	static string calculateHash(string key) {
		unchecked // Overflow is fine, just wrap
		{
			uint hash = 2166136261;
			for (int i = 0; i < key.Length; i++) {
				hash = (hash ^ key[i]) * 16777619;
			}
			return (hash % 1024).ToString("x4"); // Returns a hexadecimal string
		}
	}

	public static void DefaultSerializer(T value, Stream stream) => JsonSerializer.Serialize(stream, value);
	public static T? DefaultDeserializer(Stream stream) => JsonSerializer.Deserialize<T>(stream);
	#endregion



	#region IDictionary<string,T> Implementation
	public T this[string key] {
		get => Get(key) ?? throw new KeyNotFoundException();
		set => Set(key, value);
	}

	public ICollection<string> Keys => GetKeys();
	public ICollection<T> Values => throw new Exception("This should not be used. Too inefficient!");

	public int Count { get; private set; }
	public bool IsReadOnly => false;


	void addInternal(string key, T value) {
		string hash = MapDir<T>.calculateHash(key);

		string dirPath = Path.Combine(_rootPath, hash);
		string keysPath = Path.Combine(dirPath, KEYS_FILE_NAME);

		int keyIndex;
		string[] keys;
		if (!Directory.Exists(dirPath)) {
			Directory.CreateDirectory(dirPath);
			keys = [];
			keyIndex = -1;
		} else {
			keys = File.ReadAllLines(keysPath);
			keyIndex = Array.IndexOf(keys, key);
		}
		if (keyIndex != -1)
			throw new ArgumentException("Key already exists in the directory-map.");

		// key not found
		if (keyIndex == -1) keyIndex = keys.Length;

		string filePath = Path.Combine(dirPath, keyIndex.ToString());
		using var stream = File.Create(filePath);
		Serializer.Invoke(value, stream);

		// update keys
		File.WriteAllLines(keysPath, keys.Append(key));
		updateCount(Count + 1);
	}
	public void Add(string key, T value) {
		try {
			lock (@lock)
				addInternal(key, value);
		} catch(Exception ex) {
			ConsoleEx.Error(ex.ToString());
			throw;
		}
	}

	public bool ContainsKey(string key) {
		string hash = calculateHash(key);

		string dirPath = _rootPath + hash;
		if (!Directory.Exists(dirPath)) return false;

		string keysPath = dirPath + "/" + KEYS_FILE_NAME;
		foreach (var line in File.ReadLines(keysPath))
			if (line == key) return true;
		return false;
	}

	bool removeInternal(string key) {
		string hash = MapDir<T>.calculateHash(key);

		string dirPath = _rootPath + hash;
		string keysPath = dirPath + "/" + KEYS_FILE_NAME;

		int keyIndex;
		string[] keys;
		if (!Directory.Exists(dirPath)) {
			Directory.CreateDirectory(dirPath);
			keys = [];
			keyIndex = -1;
		} else {
			keys = File.ReadAllLines(keysPath);
			keyIndex = Array.IndexOf(keys, key);
		}
		// key not found
		if (keyIndex == -1) return false;

		// remove the index file
		string filePath = Path.Combine(dirPath, keyIndex.ToString());
		File.Delete(filePath);

        // rename all the files after
        for (int i = keyIndex; i <= keys.Length; i++)
			File.Move(getPathForIdx(dirPath, i+1), getPathForIdx(dirPath, i));

		File.WriteAllLines(keysPath, new ArraySegment<string>(keys, 0, keys.Length-1));

		// update the count
		updateCount(Count-1);

        return true;
	}
	public bool Remove(string key) {
		try {
			lock (@lock)
				return removeInternal(key);
		} catch(Exception ex) {
			ConsoleEx.Error(ex.ToString());
			throw;
		}
	}

	public bool TryGetValue(string key, [MaybeNullWhen(false)] out T value) {
		string hash = calculateHash(key);

		string dirPath = _rootPath + hash;
		if (!Directory.Exists(dirPath)) {
			value = default;
			return false;
		}

		dirPath += "/";
		string keysPath = dirPath + KEYS_FILE_NAME;
		int index = 0;
		foreach (var line in File.ReadLines(keysPath)) {
			if (line == key) {
				using var fs = File.OpenRead(dirPath + index);
                Debug.WriteLine(keysPath);
                value = Deserializer.Invoke(fs);
				return value is not null;
			}
			index++;
		}
		value = default;
		return false;
	}

	public void Add(KeyValuePair<string, T> item) => Add(item.Key, item.Value);
	public void Clear() {
		Directory.Delete(_rootPath);
		updateCount(0);
	}
	public bool Contains(KeyValuePair<string, T> item) => ContainsKey(item.Key);
	public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex) {
		throw new NotImplementedException();
	}

	public bool Remove(KeyValuePair<string, T> item) => Remove(item.Key);

	public IEnumerator<KeyValuePair<string, T>> GetEnumerator() {
		throw new Exception("Should not be used this way, it is very costly...");
	}
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	#endregion


	int readCount() => int.Parse(File.ReadAllText(_rootPath + COUNT_FILE_NAME));
	/// <summary> Updates the count of entry file. </summary>
	/// <param name="newCount"></param>
	void updateCount(int newCount) => File.WriteAllText(_rootPath + COUNT_FILE_NAME, (Count = newCount).ToString());

	static string getPathForIdx(string dirpath, int index) => Path.Combine(dirpath, index.ToString());

	/// <summary>
	/// Retrives all the keys by enumerating the directories and reading the keys file.
	/// WARNING: Costly operation (in terms of IO executions)
	/// </summary>
	public List<string> GetKeys() {
		List<string> keys = new List<string>(Count);
		foreach (string dirpath in Directory.EnumerateDirectories(_rootPath)) {
			string keysPath = Path.Combine(dirpath, KEYS_FILE_NAME);
			foreach (string key in File.ReadLines(keysPath)) {
				keys.Add(key);
			}
		}
		return keys;
	}


	T? getInternal(string key) {
		string hash = calculateHash(key);

		string dirPath = Path.Combine(_rootPath, hash);

		int keyIndex;
		string[] keys;
		if (!Directory.Exists(dirPath))
			return default;

		string keysPath = Path.Combine(dirPath, KEYS_FILE_NAME);
		keys = File.ReadAllLines(keysPath);
		keyIndex = Array.IndexOf(keys, key);
		// key not found
		if (keyIndex == -1) return default;

		string filePath = Path.Combine(dirPath, keyIndex.ToString());
		using var stream = File.OpenRead(filePath);
		return Deserializer.Invoke(stream);
	}
	public T? Get(string key) {
		try {
			lock (@lock)
				return getInternal(key);
		} catch (Exception ex) {
			ConsoleEx.Error(ex.ToString());
			throw;
		}
	}

	void setInternal(string key, T value) {
		string hash = MapDir<T>.calculateHash(key);

		string dirPath = Path.Combine(_rootPath, hash);
		string keysPath = Path.Combine(dirPath, KEYS_FILE_NAME);

		int keyIndex;
		string[] keys;
		if (!Directory.Exists(dirPath)) {
			Directory.CreateDirectory(dirPath);
			keys = [];
			keyIndex = -1;
		} else {
			keys = File.ReadAllLines(keysPath);
			keyIndex = Array.IndexOf(keys, key);
		}
		// key not found
		if (keyIndex == -1) {
			keyIndex = keys.Length;
			updateCount(Count + 1);
			// update keys
			File.WriteAllLines(keysPath, keys.Append(key));
		}

		string filePath = Path.Combine(dirPath, keyIndex.ToString());
		using var stream = File.Create(filePath);
		Serializer.Invoke(value, stream);
	}
	public void Set(string key, T value) {
		try {
			lock (@lock)
				setInternal(key, value);
		} catch(Exception ex) {
			ConsoleEx.Error(ex.ToString());
			throw;
		}
	}
}
