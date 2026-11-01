using System.Text.Json.Nodes;
using Dimensions.Definitions;

namespace Dimensions.Pipeline;

/// <summary>
/// Tagged property values in the build plan. The editor script (unreal/Python/dimensions) turns each into the
/// matching Unreal value; untagged JSON is converted using the property's current type.
/// </summary>
public static class Values
{
    public static JsonObject Asset(string objectPath) => new() { ["$asset"] = GamePath.ObjectPath(objectPath) };
    public static JsonObject Class(string classPath) => new() { ["$class"] = classPath };
    public static JsonObject Soft(string objectPath) => new() { ["$soft"] = GamePath.ObjectPath(objectPath) };
    public static JsonObject SoftClass(string classPath) => new() { ["$softClass"] = classPath };
    public static JsonObject Name(string name) => new() { ["$name"] = name };
    public static JsonObject Enum(string name) => new() { ["$enum"] = name };

    public static JsonObject Text(LocalizedText text) =>
        new() { ["$text"] = new JsonObject(text.Cultures.Select(c => KeyValuePair.Create(c.Key, (JsonNode?)c.Value))) };

    public static JsonObject New(string classPath, JsonObject properties) =>
        new() { ["$new"] = classPath, ["properties"] = properties };

    /// <summary>A struct value. <paramref name="type"/> is the struct's name as Unreal's Python sees it
    /// (EquipmentModSlot), needed when it goes into an array the editor can't infer an element type from.</summary>
    public static JsonObject Struct(string type, JsonObject fields) => new() { ["$struct"] = fields, ["$type"] = type };

    public static JsonObject RowHandle(string tableObjectPath, string row) =>
        new() { ["$rowHandle"] = new JsonObject { ["table"] = GamePath.ObjectPath(tableObjectPath), ["row"] = row } };

    public static JsonArray Array(IEnumerable<JsonNode?> items) => new([.. items]);
}
