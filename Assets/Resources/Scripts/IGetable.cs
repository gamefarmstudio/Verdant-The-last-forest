using UnityEngine;

namespace Verdant.Combat
{
    /// <summary>
    /// Shared interface for "anything that can be collected" (e.g. resources, collectibles).
    /// Keeps the pickup system independent of any one class, so collectible types can change freely.
    /// </summary>
    public interface IGetable
    {
        /// <summary>Position of the collectible (for the player to aim at / move toward).</summary>
        Transform Transform { get; }
        /// <summary>How much it is worth when collected (e.g. 1, 5, 10).</summary>
        int Amount { get; }
        /// <summary>Called when the player collects it.</summary>
        void OnCollected();
    }
}