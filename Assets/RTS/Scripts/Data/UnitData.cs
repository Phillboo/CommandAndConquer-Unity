using UnityEngine;

namespace RTS
{
    public class UnitData : ScriptableObject
    {
        public string id;
        public string displayName;
        public string modelFile;
        public int cost;
        public float buildTime = 5f;
        public int maxHp = 100;
        public float speed = 5f;
        public float damage;
        public float attackRange = 8f;
        public float attackRate = 1f;
        public float aggroRange = 10f;
        public float projectileSpeed = 25f;
        public bool isHarvester;
        public Factory builtAt;
        public float agentRadius = 0.6f;
        public float hbHeight = 2.2f;
    }
}
