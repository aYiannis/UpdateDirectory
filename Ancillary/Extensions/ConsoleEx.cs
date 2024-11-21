using System.Diagnostics.CodeAnalysis;

using static System.Console;
using static System.Environment;

namespace UpdateDirectory.Ancillary.Extensions;
public static class ConsoleEx {
	// Make sure that the logWriter will get disposed on process-exit.
	static ConsoleEx() => AppDomain.CurrentDomain.ProcessExit += (_, _) => logWriter.Close();

	static readonly StreamWriter logWriter = new(BaseDirectory + "." + AppDomain.CurrentDomain.FriendlyName + ".log");
	// Expose publicly the LogWriter
	public static StreamWriter LogWriter() => logWriter;

	public static void LogError<T>(T message) => LogError(message?.ToString() ?? "null");
	public static void LogError(string message) => File.AppendAllText(BaseDirectory + ".error.log", $"[{DateTime.Now:O}]: {message}");

	/// <summary> It writes the message in red colore and beeps. </summary>
	/// <param name="message"> The message to display. </param>
	/// <param name="exitCode"> The exit code with witch the program should exit after (0 for not exit). </param>
	public static void Error(string message) {
		LogError(message + "\n");
		WriteColored(message + "\n", ConsoleColor.Red);
		Beep();
	}

	[DoesNotReturn]
	public static void Error(string message, int exitCode = 1) {
		Error(message);
		// Make sure that you keep the console alive for the user to see the message,
		//   because you're going to close the application after that.
		ReadLine();
		Exit(exitCode);
	}

	static ConsoleColor emphaticColor = ConsoleColor.DarkCyan;
	/// <summary> The color with witch the class defines emphasis. </summary>
	public static ConsoleColor EmphaticColor {
		get => emphaticColor;
		set => emphaticColor = value;
	}
	/// <summary> Writes a message toggling emphasis to the parameters. </summary>
	public static void WriteEmphatic(params Span<string?> phrases) {
		ConsoleColor oColor = ForegroundColor;
		int len = phrases.Length;
		for (int i = 0; i < len; i++) {
			ForegroundColor = i % 2 == 0 ? oColor : emphaticColor;
			Write(phrases[i] ?? "null");
		}
		if (len % 2 == 1) ForegroundColor = oColor;
		WriteLine();
	}

	#region Console Replacements
	public static void Write(object? message) => Write(message?.ToString() ?? "null");

	public static void Write(string message) {
		logWriter.Write(message);
		Console.Write(message);
	}

	public static void WriteLine(string message) {
		logWriter.WriteLine(message);
		Console.WriteLine(message);
	}

	public static void WriteLine() {
		logWriter.WriteLine();
		Console.WriteLine();
	}
	#endregion


	/// <summary> Writes a message with the given color and returns the console color to its original value. </summary>
	public static void WriteColored(string message) => WriteColored(message, EmphaticColor);
		/// <summary> Writes a message with the given color and returns the console color to its original value. </summary>
	public static void WriteColored(string message, ConsoleColor color) {
		var oColor = ForegroundColor;
		ForegroundColor = color;
		Write(message);
		ForegroundColor = oColor;
	}

	public static T Input<T>(string? query = null, IFormatProvider? formatProvider = null) where T : IParsable<T> {
		if (query != null) Write(query);
		Write(": ");

		var oColor = ForegroundColor;

		while (true) {
			ForegroundColor = emphaticColor;
			string? answer = ReadLine();
			ForegroundColor = oColor;

			if (T.TryParse(answer, formatProvider, out T? result) && result is not null) {
				return result;
			} else {
				Error("Invalid input. Please try again.");
			}
		}
	}

	/// <summary> Ask a yes or no question the user. The default result is false. </summary>
	/// <param name="query"> The Y/N question. </param>
	/// <param name="strict"> Determins wether or not exactly the key N is required or any non Y is considered as false. </param>
	public static bool Ask(string query, bool strict = false) {
		Write(query);
		WriteColored(" [y/n]");
		Write(":");

		// Maximum number of retries 100. After that, give up.
		for (int i = 0; i < 99; i++) {
			var key = ReadKey().Key;
			WriteLine();
			switch (key) {
				case ConsoleKey.Y: return true;
				case ConsoleKey.N: return false;
				default:
					if (!strict) return false;
					break;
			}
			// To many missclicks, inform with message in addition to the beep.
			if (i % 10 == 9) Error("Invalid hotkey!", 0);
		}
		return false;
	}

	/// <summary> The swapping colors for the given choices. </summary>
	public static readonly ConsoleColor[] ColorsForOptions = [ConsoleColor.DarkCyan, ConsoleColor.Gray, ConsoleColor.White];

	/// <summary> Select one of the given options (max: 9). </summary>
	/// <param name="query"> The primary question, can be null. </param>
	/// <param name="options"></param>
	/// <returns></returns>
	public static (T? Choice, int Index) Choose<T>(string query, params T[] options) {
		if (options.Length < 2) throw new ArgumentOutOfRangeException("Too few options to chose from. (" + options.Length + ")");
		if (options.Length > 9) throw new ArgumentOutOfRangeException($"Too many options ({options.Length}), how is the user supose to chose.");

		if (query is not null) WriteLine(query);

		// Lay the options
		for (int i = 0; i < options.Length; i++) {
			ForegroundColor = ColorsForOptions[i % ColorsForOptions.Length];
			WriteLine($"{i + 1,2}. {options[i]}");
		}

		for (int i = 0; i < 999; i++) {
			var key = ReadKey().Key;
			WriteLine();
			switch (key) {
				case ConsoleKey.NumPad1:
				case ConsoleKey.D1:
					if (options.Length <= 0) break;
					return (options[0], 0);
				case ConsoleKey.NumPad2:
				case ConsoleKey.D2:
					if (options.Length <= 1) break;
					return (options[1], 1);
				case ConsoleKey.NumPad3:
				case ConsoleKey.D3:
					if (options.Length <= 2) break;
					return (options[2], 2);
				case ConsoleKey.NumPad4:
				case ConsoleKey.D4:
					if (options.Length <= 3) break;
					return (options[3], 3);
				case ConsoleKey.NumPad5:
				case ConsoleKey.D5:
					if (options.Length <= 4) break;
					return (options[4], 4);
				case ConsoleKey.NumPad6:
				case ConsoleKey.D6:
					if (options.Length <= 5) break;
					return (options[5], 5);
				case ConsoleKey.NumPad7:
				case ConsoleKey.D7:
					if (options.Length <= 6) break;
					return (options[6], 6);
				case ConsoleKey.NumPad8:
				case ConsoleKey.D8:
					if (options.Length <= 7) break;
					return (options[7], 7);
				case ConsoleKey.NumPad9:
				case ConsoleKey.D9:
					if (options.Length <= 8) break;
					return (options[8], 8);
			}
			// To many missclicks, inform with message in addition to the beep.
			if (i % 10 == 9) Error("Invalid hotkey!", 0);
		}
		// the loop reached its limit
		Error("Too many invalid hotkeys. Aborting...");
		return (default(T), -1);
	}

	public static int Select<T>(IEnumerable<T> options) {
		ArgumentNullException.ThrowIfNull(options);

		CursorVisible = false;

		int count = 1;
		using (var en = options.GetEnumerator()) {
			if (!en.MoveNext()) throw new Exception("Empty options collection!");
			Write("> " + en.Current + ".\n");
			while (en.MoveNext()) {
				Write("  " + en.Current + ".\n");
				count++;
			}
		}
		Write("  Exit.");

		int index = 0;
		CursorTop -= count;
		while (true) switch (ReadKey(true).Key) {
				case ConsoleKey.PageUp:
					if (index <= 0) break;
					move(-Math.Min(index, 4));
					break;
				case ConsoleKey.PageDown:
					if (index >= count) break;
					move(Math.Min(count - index, 4));
					break;
				case ConsoleKey.UpArrow:
					if (index <= 0) break;
					move(-1);
					break;
				case ConsoleKey.DownArrow:
					if (index >= count) break;
					move();
					break;
				case ConsoleKey.Enter:
					CursorVisible = true;
					CursorLeft = 0;
					CursorTop += count - index + 1;
					return index == count ? -1 : index;
				case ConsoleKey.Escape:
					CursorVisible = true;
					CursorLeft = 0;
					CursorTop += count - index + 1;
					return -1;
			}
		void move(int steps = 1) {
			if (steps == 0) return;

			CursorLeft = 0;
			Write(' ');
			CursorTop += steps;
			index += steps;
			CursorLeft = 0;
			Write('>');
		}
	}
}