// ═══════════════════════════════════════════════════════════════
//  GameEvents.cs — Verdant's static event bus
//  Pollution system: every system talks through these events
//  instead of referencing each other directly.
// ═══════════════════════════════════════════════════════════════
using System;

/// <summary>
/// Global static events. Raise them with the Raise* methods; subscribe with += / -=.
/// </summary>
public static class GameEvents
{
    // ── Pollution ──
    public static event Action<float> OnPollutionChanged;   // new gauge value, 0..1
    public static event Action OnPollutionCleared;           // fog reached 0% (no longer a win condition)
    public static event Action OnPollutionFull;              // fog reached 100% -> game over

    public static event Action<float> OnEnemyKilled;         // passes the cleanse amount of the enemy that died
    public static event Action<float> OnCrystalDamaged;      // passes the damage the crystal took

    public static void RaisePollutionChanged(float v) => OnPollutionChanged?.Invoke(v);
    public static void RaisePollutionCleared()        => OnPollutionCleared?.Invoke();
    public static void RaisePollutionFull()           => OnPollutionFull?.Invoke();
    public static void RaiseEnemyKilled(float cleanse) => OnEnemyKilled?.Invoke(cleanse);
    public static void RaiseCrystalDamaged(float dmg)  => OnCrystalDamaged?.Invoke(dmg);
}
