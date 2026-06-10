using UnityEngine;

namespace RTS
{
    public class GameBootstrap : MonoBehaviour
    {
        void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var yard = gm.GetBuilding("ConYard");
            var harv = gm.GetUnit("Harvester");
            var inf = gm.GetUnit("Infantry");
            var tank = gm.GetUnit("TankLight");

            // Spieler unten links
            gm.SpawnBuilding(yard, Team.Player, new Vector3(-70f, 0f, -70f), 45f, true);
            gm.SpawnUnit(harv, Team.Player, new Vector3(-63f, 0f, -68f));
            gm.SpawnUnit(inf, Team.Player, new Vector3(-65f, 0f, -62f));
            gm.SpawnUnit(inf, Team.Player, new Vector3(-62f, 0f, -65f));
            gm.SpawnUnit(tank, Team.Player, new Vector3(-60f, 0f, -60f));

            // KI oben rechts
            gm.SpawnBuilding(yard, Team.Enemy, new Vector3(70f, 0f, 70f), 225f, true);
            gm.SpawnUnit(harv, Team.Enemy, new Vector3(63f, 0f, 68f));
            gm.SpawnUnit(inf, Team.Enemy, new Vector3(65f, 0f, 62f));
            gm.SpawnUnit(inf, Team.Enemy, new Vector3(62f, 0f, 65f));
            gm.SpawnUnit(tank, Team.Enemy, new Vector3(60f, 0f, 60f));

            UIManager.Message("Errichte deine Basis und vernichte den Gegner!");
        }
    }
}
