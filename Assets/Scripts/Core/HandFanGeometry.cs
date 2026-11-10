using System;

namespace Reborn.Core
{
    /// <summary>Unity-independent pose for the up-to-five-card fan in table units.</summary>
    public readonly struct HandFanPose
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float Yaw;

        public HandFanPose(float x, float y, float z, float yaw)
        {
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
        }
    }

    /// <summary>
    /// Shared hand geometry for the 2.5D renderer. Card order remains from
    /// left to right (0 to count-1); the fan's curvature and rotation are
    /// opposite to those in Prototype 0.14.
    ///
    /// Expanded spacing is 1.05 + 0.90 = 1.95 table units, per the
    /// approved UI request. Compact layout pulls cards down/together.
    /// </summary>
    public static class HandFanGeometry
    {
        public const float CardWidth = 1.90f;
        public const float CardLength = 2.55f;
        public const float CompactSpacing = 1.15f;
        public const float ExpandedSpacing = 1.95f;
        public const float CompactBaseY = 0.44f;
        public const float ExpandedBaseY = 0.58f;
        public const float CompactBaseZ = -7.52f;
        public const float ExpandedBaseZ = -7.28f;

        /// <summary>
        /// On expanded hand hover, move only the neighboring cards away from
        /// the highlighted card. Index/order stay intact and the focused card
        /// retains its base position for drag and Shift-fusion interaction.
        /// No displacement is applied while the pointer is over the arena.
        /// </summary>
        public static HandFanPose WithHoveredCard(
            int index, int count, bool expanded, int hoveredIndex)
        {
            var pose = At(index, count, expanded);
            if (hoveredIndex < -1 || hoveredIndex >= count)
                throw new ArgumentOutOfRangeException(nameof(hoveredIndex));
            if (!expanded || hoveredIndex < 0 || index == hoveredIndex)
                return pose;

            int separation = Math.Abs(index - hoveredIndex);
            float displacement = separation == 1 ? 0.30f :
                separation == 2 ? 0.14f : 0.06f;
            float direction = index < hoveredIndex ? -1f : 1f;
            return new HandFanPose(pose.X + direction * displacement,
                pose.Y, pose.Z, pose.Yaw);
        }

        public static HandFanPose At(int index, int count, bool expanded)
        {
            if (count < 1 || count > 5)
                throw new ArgumentOutOfRangeException(nameof(count), "Expected 1..5 cards.");
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));

            float spread = index - (count - 1) * 0.5f;
            float spacing = expanded ? ExpandedSpacing : CompactSpacing;
            float baseY = expanded ? ExpandedBaseY : CompactBaseY;
            float baseZ = expanded ? ExpandedBaseZ : CompactBaseZ;
            float depth = expanded ? 0.16f : 0.08f;
            float angle = expanded ? 10f : 6f;

            // Outer edges curve toward the player, not toward the battlefield.
            // Positive yaw is the inverse of the old -9.5deg/card fan.
            return new HandFanPose(
                spread * spacing,
                baseY + Math.Abs(spread) * 0.01f,
                baseZ - Math.Abs(spread) * depth,
                spread * angle);
        }
    }
}
