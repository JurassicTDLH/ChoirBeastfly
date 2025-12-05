using System.Linq;

namespace ChoirBeastFly;

/// <summary>
/// Contains extension helper methods.
/// </summary>
internal static class Extensions
{
    /// <summary>
    /// Get the root asset name from its path.
    /// </summary>
    /// <param name="assetPath">The full path to the asset.</param>
    /// <returns>The trimmed asset name.</returns>
    internal static string GetAssetRoot(this string assetPath) =>
        assetPath.Split("/").Last().Replace(".asset", "").Replace(".prefab", "").Replace(".wav", "");
}
