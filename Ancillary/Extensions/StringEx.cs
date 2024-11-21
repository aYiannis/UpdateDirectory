using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;


namespace UpdateDirectory.Ancillary.Extensions;
public static class StringEx {
	/// <summary> Remove-diacritics. </summary>
	public static string RemoveDiacritics(this string self) {
		if (string.IsNullOrWhiteSpace(self)) return self;

		self = self.Normalize(NormalizationForm.FormD);
		var chars = self.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
		return new string(chars).Normalize(NormalizationForm.FormC);
	}

	/// <summary> Converts the string to upper case and removes the diacritics. </summary>
	public static string ToKey(this string self) => self.ToUpper().RemoveDiacritics().Trim();

	/// <summary> A better looking string.Join(IEnumerable, glue=",") </summary>
	public static string Join<T>(this IEnumerable<T> self, string glue = ",") => string.Join(glue, self);

#if !NO_JSON
	public static JsonSerializerOptions JSO { get; set; } = new() {
		PropertyNameCaseInsensitive = true,
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		AllowTrailingCommas = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};
	public static string ToJson<T>(this T self, JsonSerializerOptions? jso = null) => JsonSerializer.Serialize(self, jso ?? JSO);
	public static T? FromJson<T>(this string self) where T : class
		=> JsonSerializer.Deserialize<T>(self) ?? null;
#endif

	public static bool IsAt(this string self, string keyword, int index) {
		if (index > self.Length) return false;
		int length = keyword.Length;
		for (int i = 0; i < length; i++) {
			if (self[index + i] != keyword[i])
				return false;
		}
		return true;
	}
}