using System;
using System.Collections.Generic;

namespace LiveGameDev.ESS.Editor
{
    /// <summary>
    /// Snapshot of economy state at a single day.
    /// </summary>
    [Serializable]
    public class SimState
    {
        public int Day { get; set; }

        public float TotalCurrency { get; set; }

        public float GiniCoefficient { get; set; }

        /// <summary>Item name → current price.</summary>
        public Dictionary<string, float> ItemPrices { get; } = new();

        /// <summary>Item name → total supply in economy.</summary>
        public Dictionary<string, float> ItemSupply { get; } = new();

        /// <summary>Create a deep copy of this state.</summary>
        public SimState Clone()
        {
            var copy = new SimState
            {
                Day = Day,
                TotalCurrency = TotalCurrency,
                GiniCoefficient = GiniCoefficient
            };
            foreach (var kvp in ItemPrices)
                copy.ItemPrices[kvp.Key] = kvp.Value;
            foreach (var kvp in ItemSupply)
                copy.ItemSupply[kvp.Key] = kvp.Value;
            return copy;
        }
    }
}