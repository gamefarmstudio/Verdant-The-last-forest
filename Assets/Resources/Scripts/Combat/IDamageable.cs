using UnityEngine;

namespace Verdant.Combat
{
    /// <summary>
    /// Shared interface for "anything that can take damage" (enemies, etc.).
    /// Towers and projectiles only talk to enemies through this interface,
    /// so the firing system isn't tied to any one enemy class — enemies can change freely.
    ///
    /// In Verdant: EnemyBase implements this interface.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>The target's position (projectiles home in on it).</summary>
        Transform Transform { get; }

        /// <summary>Whether it is still alive.</summary>
        bool IsAlive { get; }

        /// <summary>Current health (used by the Strongest targeting mode).</summary>
        float CurrentHealth { get; }

        /// <summary>Progress along the path, 0..1 (higher = closer to our base). Used by the First/Last targeting modes.</summary>
        float PathProgress { get; }

        /// <summary>Applies damage.</summary>
        void TakeDamage(float amount);
    }
}
