using UnityEngine;

namespace RTS
{
    public class BuildingData : ScriptableObject
    {
        public string id;
        public string displayName;
        public string modelFile;
        public int cost;
        public float buildTime = 8f;
        public int maxHp = 500;
        public int energyDelta;
        public Vector2 footprint = new Vector2(4f, 4f);
        public bool isDefense;
        public bool buildable = true;
        public float damage = 20f;
        public float attackRange = 13f;
        public float attackRate = 0.8f;
        public float hbHeight = 4f;
    }
}
