using System.Text.RegularExpressions;

namespace Dimensions.Definitions;

/// <summary>Helpers for telling Unreal object paths apart from definition ids and file paths.</summary>
public static partial class GamePath
{
    [GeneratedRegex(@"^/(Game|Script)/\S+$")]
    private static partial Regex GamePathRegex();

    [GeneratedRegex(@"^[A-Z][A-Za-z0-9_]{1,63}$")]
    private static partial Regex IdRegex();

    public static bool IsGamePath(string value) => GamePathRegex().IsMatch(value);
    public static bool IsId(string value) => IdRegex().IsMatch(value);
    public static bool IsNative(string value) => value.StartsWith("/Script/", StringComparison.Ordinal);

    /// <summary>"/Game/A/B.B_C" -> "/Game/A/B" (the package). Native paths are returned as they are.</summary>
    public static string PackageOf(string objectPath)
    {
        if (IsNative(objectPath)) return objectPath;
        var dot = objectPath.IndexOf('.');
        return dot < 0 ? objectPath : objectPath[..dot];
    }

    /// <summary>"/Game/A/B" -> "/Game/A/B.B" (the asset object in its package).</summary>
    public static string ObjectPath(string packagePath) =>
        packagePath.Contains('.') ? packagePath : $"{packagePath}.{packagePath[(packagePath.LastIndexOf('/') + 1)..]}";

    /// <summary>"/Game/A/B" -> "/Game/A/B.B_C" (a blueprint's generated class).</summary>
    public static string ClassPath(string packagePath) => $"{ObjectPath(PackageOf(packagePath))}_C";
}
