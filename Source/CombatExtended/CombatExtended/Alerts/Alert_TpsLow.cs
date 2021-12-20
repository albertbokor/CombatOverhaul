using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
#if DEBUG
	public class Alert_TpsLow : Alert
	{
        public override Color BGColor
        {
            get
            {
                Color color;
                if (PerformanceTracker.TpsCriticallyLow)
                {
                    color = Color.red;
                }
                else
                {
                    color = Color.green;
                }
                color.a = 0.2f;
                return color;
            }
        }

        public override AlertPriority Priority
        {
            get => AlertPriority.Critical;
        }        

        public Alert_TpsLow()
		{
            defaultLabel = "";
            defaultExplanation = "";
        }        

        public override string GetLabel()
        {
            if (PerformanceTracker.TpsCriticallyLow)
            {
                return $"DEV:TPS[{Math.Round(PerformanceTracker.TpsLevel, 2) * 100}%][LOW!]";
            }
            else
            {
                return $"DEV:TPS[{Math.Round(PerformanceTracker.TpsLevel, 2) * 100}%][NRO!]";
            }
        }

        public override TaggedString GetExplanation()
        {
            if (PerformanceTracker.TpsCriticallyLow)
            {
                return $"DEV: TPS factor is at <color=orange>{Math.Round(PerformanceTracker.TpsLevel, 2) * 100}%</color>. <color=red>Tps is critically low</color> and CE will throttle down systems to improve performance.";
            }
            else
            {
                return $"DEV: TPS factor is at <color=orange>{Math.Round(PerformanceTracker.TpsLevel, 2) * 100}%</color>. <color=green>Tps is normal</color> and CE will try to take advantage of the performance headspace.";
            }
        }

        public override AlertReport GetReport()
        {
            return AlertReport.Active;
        }
    }
#endif
}

