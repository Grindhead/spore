using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Tracks the player's combat/feeding behaviour between reproductions and exposes
    /// a <see cref="BehaviourBias"/> used to skew the three mutation variants shown at
    /// reproduction time (see <see cref="MutationSystem.MutateBiased"/>).
    ///
    /// Design: natural-selection pressure — if the player hunts a lot, Attack/Spikes
    /// become more likely to increase; if they get hit often, Defence rises; etc.
    /// </summary>
    public class BehaviourBiasTracker
    {
        // --- counters (reset after each reproduction)
        private int _plantEaten;
        private int _meatEaten;
        private int _toxinEaten;
        private int _cellsConsumed;
        private int _damageTaken;
        private int _predatorsEscaped;

        // --- public read-only accessors (used in tests)
        public int PlantEaten      => _plantEaten;
        public int MeatEaten       => _meatEaten;
        public int ToxinEaten      => _toxinEaten;
        public int CellsConsumed   => _cellsConsumed;
        public int DamageTaken     => _damageTaken;
        public int PredatorsEscaped => _predatorsEscaped;

        // --- record events

        public void RecordFoodEaten(FoodType type)
        {
            switch (type)
            {
                case FoodType.Plant: _plantEaten++;   break;
                case FoodType.Meat:  _meatEaten++;    break;
                case FoodType.Toxin: _toxinEaten++;   break;
            }
        }

        public void RecordCellConsumed()  => _cellsConsumed++;
        public void RecordDamageTaken()   => _damageTaken++;
        public void RecordPredatorEscaped() => _predatorsEscaped++;

        /// <summary>Reset all counters (call after reproduction so the next generation starts fresh).</summary>
        public void Reset()
        {
            _plantEaten = _meatEaten = _toxinEaten = 0;
            _cellsConsumed = _damageTaken = _predatorsEscaped = 0;
        }

        /// <summary>
        /// Returns a <see cref="BehaviourBias"/> reflecting the dominant play style since last reset.
        /// Each field is a delta bias in the range roughly [-1, +1]; positive means that trait
        /// is more likely to increase during biased mutation, negative means more likely to decrease.
        /// </summary>
        public BehaviourBias ComputeBias()
        {
            var b = new BehaviourBias();

            // Herbivore (plant eater) → better digestion and speed to find more food
            if (_plantEaten >= 4)
            {
                b.Digestion += Scale(_plantEaten, 4, 15) * 0.8f;
                b.Speed     += Scale(_plantEaten, 4, 15) * 0.4f;
                b.Flagella  += Scale(_plantEaten, 4, 15) * 0.3f;
            }

            // Carnivore (meat eater) → attack, spikes, size
            if (_meatEaten >= 2)
            {
                b.Attack  += Scale(_meatEaten, 2, 10) * 0.7f;
                b.Spikes  += Scale(_meatEaten, 2, 10) * 0.6f;
                b.Size    += Scale(_meatEaten, 2, 10) * 0.4f;
            }

            // Cell consumer (predation) → attack, size
            if (_cellsConsumed >= 2)
            {
                b.Attack += Scale(_cellsConsumed, 2, 8) * 0.6f;
                b.Size   += Scale(_cellsConsumed, 2, 8) * 0.5f;
            }

            // Toxin eater → digestion, defence (chemical resistance)
            if (_toxinEaten >= 1)
            {
                b.Digestion += Scale(_toxinEaten, 1, 6) * 0.6f;
                b.Defence   += Scale(_toxinEaten, 1, 6) * 0.4f;
            }

            // Took lots of damage → defence, spikes (deterrence)
            if (_damageTaken >= 3)
            {
                b.Defence += Scale(_damageTaken, 3, 12) * 0.8f;
                b.Spikes  += Scale(_damageTaken, 3, 12) * 0.3f;
            }

            // Escaped predators → speed, flagella
            if (_predatorsEscaped >= 1)
            {
                b.Speed    += Scale(_predatorsEscaped, 1, 5) * 0.8f;
                b.Flagella += Scale(_predatorsEscaped, 1, 5) * 0.6f;
            }

            return b;
        }

        /// <summary>
        /// Returns a human-readable description of the dominant bias, shown in the mutation choice UI.
        /// Returns null when no clear bias exists.
        /// </summary>
        public string GetBiasDescription()
        {
            var b = ComputeBias();
            float maxBias = 0f;
            string desc = null;

            Check(b.Attack + b.Spikes,   ref maxBias, ref desc, "Your aggressive hunting has favored Attack and Spike traits.");
            Check(b.Speed + b.Flagella,  ref maxBias, ref desc, "Constant movement has favored Speed and Flagella traits.");
            Check(b.Defence,             ref maxBias, ref desc, "Surviving damage has favored Defence traits.");
            Check(b.Digestion,           ref maxBias, ref desc, "Your diet has favored Digestion traits.");
            Check(b.Size,                ref maxBias, ref desc, "Your size advantage has favored Growth traits.");

            return maxBias > 0.2f ? desc : null;
        }

        private static float Scale(int count, int min, int max) =>
            Mathf.Clamp01((count - min) / (float)(max - min));

        private static void Check(float value, ref float max, ref string desc, string message)
        {
            if (value > max) { max = value; desc = message; }
        }
    }

    /// <summary>Per-trait bias values used by <see cref="MutationSystem.MutateBiased"/>.</summary>
    public struct BehaviourBias
    {
        public float Speed;
        public float Size;
        public float Attack;
        public float Defence;
        public float Digestion;
        public float Spikes;
        public float Flagella;
    }
}
