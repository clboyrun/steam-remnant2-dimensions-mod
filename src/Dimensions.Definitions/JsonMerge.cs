using System.Text.Json.Nodes;

namespace Dimensions.Definitions;

public static class JsonMerge
{
    /// <summary>
    /// Returns a copy of <paramref name="baseObject"/> with <paramref name="overlay"/> on top: objects merge key by
    /// key, every other value (arrays included) is replaced. A null in the overlay removes the key.
    /// </summary>
    public static JsonObject Merge(JsonObject baseObject, JsonObject overlay)
    {
        var result = (JsonObject)baseObject.DeepClone();
        foreach (var (key, value) in overlay)
        {
            if (value is null)
            {
                result.Remove(key);
            }
            else if (value is JsonObject overlayChild && result[key] is JsonObject baseChild)
            {
                result[key] = Merge(baseChild, overlayChild);
            }
            else
            {
                result[key] = value.DeepClone();
            }
        }
        return result;
    }
}
