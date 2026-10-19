using UnityEngine;

/// <summary>
/// Stores the game speed the player chose (1x / 2x) in one place.
/// Anything that needs to restore Time.timeScale (e.g. TowerBuildPopup / TowerUpgradePopup when
/// a menu closes, GameManager on Resume, Tutorial when it closes) reads it from here instead of
/// hard-coding 1f — so the chosen speed is never silently reset.
/// </summary>
public static class GameSpeed
{
    public static float Current { get; private set; } = 1f;

    /// <summary>Sets a new speed and applies it immediately (called by the speed toggle button).</summary>
    public static void SetSpeed(float speed)
    {
        Current = speed;
        Time.timeScale = speed;
    }

    /// <summary>Puts timeScale back to the current speed — call this when closing a menu/popup that temporarily overrode timeScale.</summary>
    public static void RestoreCurrent() => Time.timeScale = Current;
}
