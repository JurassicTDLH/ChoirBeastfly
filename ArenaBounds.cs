namespace ChoirBeastFly;

/// <summary>
/// Defines positions within the arena.
/// </summary>
internal static class ArenaBounds
{
    /// <summary>
    /// The arena's left edge.
    /// </summary>
    internal const float XMin = 0;
    /// <summary>
    /// The arena's right edge.
    /// </summary>
    internal const float XMax = 80;
    /// <summary>
    /// The arena's floor/
    /// </summary>
    internal const float YMin = 2;
    /// <summary>
    /// The arena's ceiling.
    /// </summary>
    internal const float YMax = 18;

    /// <summary>
    /// The horizontal center of the arena.
    /// </summary>
    internal const float XCenter = XMin + (XMax - XMin) / 2;
    /// <summary>
    /// The vertical center of the arena.
    /// </summary>
    internal const float YCenter = YMin + (YMax - YMin) / 2;
}