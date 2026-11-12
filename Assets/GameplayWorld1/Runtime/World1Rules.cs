using System;

namespace KeyboardEscapeGameplay
{
    /// <summary>W1 formulas recovered from the exact APK; source VAs are recorded below.</summary>
    public static class World1Rules
    {
        public const float MinimumMoveSpeed = 4.5f;
        public static float RealSpeedToDisplay(float speed) => Math.Max(1, (float)Math.Floor(speed * 2) * 2.5f - 14.5f);
        // Game.Factory_SpeedKeyboard.DisplaySpeedToReal, native VA 0x4e6a3cc.
        public static float DisplaySpeedToReal(float display) { return (display + 14.5f) / 5f; }

        // GetPointToNextLevel, native VA 0x4e6b4ec; factory serialized curves121/421.
        public static double GetPointToNextLevel(int level)
        {
            level = Math.Max(0, level);
            double accumulated = 100;
            double multiplier = 1.147f;
            int previous = 0;
            if (level >= 121)
            {
                accumulated *= Math.Pow(multiplier, 120);
                previous = 120;
                multiplier = 1.07f;
            }
            if (level >= 421)
            {
                accumulated *= Math.Pow(multiplier, 300);
                previous = 420;
                multiplier = 1.12f;
            }
            return accumulated * Math.Pow(multiplier, level - previous);
        }

        // SKB_Character.ApplyActualSpeed, VA0x4d9b5dc; FactoryCharacterKC4.5/W1additive.4/maxDisplay300.
        public static float MoveSpeedAtLevel(int level)
        {
            float additive = Math.Max(0, (level - 1) * .4f);
            return 4.5f + Math.Min(additive, DisplaySpeedToReal(300) - 4.5f);
        }
    }
}
