namespace UpdateDirectory.Ancillary.Extensions;
public static class ArrayEx {
	public static void Deconstruct<T>(this T[] array, out T item1, out T item2) {
		item1 = array[0];
		item2 = array[1];
	}
}
