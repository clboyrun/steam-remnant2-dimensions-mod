using Dimensions.Definitions;

namespace Dimensions.Cli;

/// <summary>Writes a new dimension folder: manifest, one example weapon, and local copies of the schemas.</summary>
internal static class Scaffold
{
    public static void Write(string folder, string id)
    {
        Directory.CreateDirectory(Path.Combine(folder, "content", "weapons"));
        Directory.CreateDirectory(Path.Combine(folder, "art"));
        Directory.CreateDirectory(Path.Combine(folder, "models"));
        WriteSchemas(Path.Combine(folder, ".schema"));

        File.WriteAllText(Path.Combine(folder, DimensionManifest.FileName), $$"""
            {
              "$schema": "./.schema/dimension.schema.json",
              "id": "{{id}}",
              "name": "{{id}}",
              "version": "0.1.0",
              "content": ["content/**/*.json"],
              "pak": { "chunkId": 7100, "format": "iostore" }
            }
            """ + "\n");

        File.WriteAllText(Path.Combine(folder, "content", "weapons", "FirstGun.json"), """
            {
              "$schema": "../../.schema/definition.schema.json",
              "kind": "weapon",
              "id": "FirstGun",
              "weaponType": "longGun",
              "name": "First Gun",
              "description": "Replace this with your own weapon.",
              "stats": { "damage": 40, "rateOfFire": 3.0, "magazine": 8, "maxAmmo": 64, "range": 45, "falloffStart": 20, "falloffEnd": 40 }
            }
            """ + "\n");
    }

    /// <summary>Local copies of the schemas, so editors such as VS Code give autocomplete without the repo.</summary>
    public static void WriteSchemas(string folder)
    {
        Directory.CreateDirectory(folder);
        foreach (var name in SchemaValidator.SchemaNames())
            File.WriteAllText(Path.Combine(folder, name), SchemaValidator.ReadSchema(name));
    }
}
