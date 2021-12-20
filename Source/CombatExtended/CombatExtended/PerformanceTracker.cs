using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class PerformanceTracker : GameComponent
    {
        public static PerformanceTracker instance;

        /// <summary>
        /// The mod perfomance level. A float ranging from 0 to 1.0f. the lower the value the worse is the current performance.
        /// Used to help maintian sustained performance.
        /// </summary>
        private static float tpsLevel = 1.0f;
        public static float TpsLevel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => tpsLevel;
        }

        private static bool tpsCriticallyLow = false;
        public static bool TpsCriticallyLow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => tpsCriticallyLow;
        }

        private float avgTickTimeMs = 0.016f;
        private Stopwatch stopwatch = new Stopwatch();
        /// <summary>
        /// Rolling avrage frame time.
        /// </summary>
        public float AvgTickTimeMs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => avgTickTimeMs;
        }
        /// <summary>
        /// The expected ticks per second from the rolling average.
        /// </summary>
        public float ExpectedTps
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Mathf.Min(1f / AvgTickTimeMs, TargetTps);
        }
        /// <summary>
        /// The expected tps deficit.
        /// </summary>
        public float TpsDeficit
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Mathf.Max(TargetTps - ExpectedTps, 0f);
        }
        /// <summary>
        /// The current target Tps.
        /// </summary>
        public float TargetTps
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Mathf.Min(Find.TickManager.TickRateMultiplier * 60f, 90f);
        }

        public PerformanceTracker(Game game)
        {
            instance = this;           
        }

        public override void GameComponentTick()
        {            
            base.GameComponentTick();
            if (!stopwatch.IsRunning)
            {
                stopwatch.Start();
                return;
            }
            float deltaT = (float)stopwatch.ElapsedTicks / Stopwatch.Frequency;           
            stopwatch.Restart();
            if (deltaT > 0.5f)
            {
                stopwatch.Stop();
                return;
            }
            avgTickTimeMs = (avgTickTimeMs * 44f + deltaT) / 45f;
            tpsLevel = Mathf.Clamp01((instance.ExpectedTps < 55f ? 0.5f : 1.0f) * (1f - instance.TpsDeficit / (instance.TargetTps + 1)));
            tpsCriticallyLow = TpsLevel < 0.667f && instance.ExpectedTps <= 50f;
        }
    }
}

