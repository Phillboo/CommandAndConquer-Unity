using UnityEngine;

namespace RTS
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }
        readonly int[] credits = { 3000, 4000 };

        void Awake() { Instance = this; }

        public int GetCredits(Team t) => credits[(int)t];
        public void Add(Team t, int amount) { credits[(int)t] += amount; }

        public bool TrySpend(Team t, int amount)
        {
            if (credits[(int)t] < amount) return false;
            credits[(int)t] -= amount;
            return true;
        }

        public int GetEnergy(Team t)
        {
            int sum = 0;
            foreach (var d in Damageable.All)
                if (d != null && d.team == t && d is Building b && b.Constructed && b.data != null)
                    sum += b.data.energyDelta;
            return sum;
        }

        public bool HasPower(Team t) => GetEnergy(t) >= 0;

        public void GetEnergyDetail(Team t, out int prod, out int cons)
        {
            prod = 0; cons = 0;
            foreach (var d in Damageable.All)
                if (d != null && d.team == t && d is Building b && b.Constructed && b.data != null)
                {
                    if (b.data.energyDelta >= 0) prod += b.data.energyDelta;
                    else cons -= b.data.energyDelta;
                }
        }
    }
}
