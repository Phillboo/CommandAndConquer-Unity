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
            gm.SpawnBuilding(yard, Team.Player, new Vector3(-38f, 0f, -38f), 45f, true);
            gm.SpawnUnit(harv, Team.Player, new Vector3(-32f, 0f, -36f));
            gm.SpawnUnit(inf, Team.Player, new Vector3(-33f, 0f, -31f));
            gm.SpawnUnit(inf, Team.Player, new Vector3(-31f, 0f, -33f));
            gm.SpawnUnit(tank, Team.Player, new Vector3(-29f, 0f, -29f));

            // KI oben rechts
            gm.SpawnBuilding(yard, Team.Enemy, new Vector3(38f, 0f, 38f), 225f, true);
            gm.SpawnUnit(harv, Team.Enemy, new Vector3(32f, 0f, 36f));
            gm.SpawnUnit(inf, Team.Enemy, new Vector3(33f, 0f, 31f));
            gm.SpawnUnit(inf, Team.Enemy, new Vector3(31f, 0f, 33f));
            gm.SpawnUnit(tank, Team.Enemy, new Vector3(29f, 0f, 29f));

            UIManager.Message("Errichte deine Basis und vernichte den Gegner!");
        }
    }
}
