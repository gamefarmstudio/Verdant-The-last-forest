namespace Verdant.Combat
{
    /// <summary>
    /// How a tower picks its target (the standard tower-defense options).
    /// </summary>
    public enum TargetingMode
    {
        First,     // furthest along the path (closest to our base) — the usual TD default
        Last,      // the one that most recently entered range
        Closest,   // nearest to the tower
        Strongest  // highest current health
    }
}
