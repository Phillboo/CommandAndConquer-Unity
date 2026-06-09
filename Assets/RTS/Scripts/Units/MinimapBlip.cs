using UnityEngine;

namespace RTS
{
    public class MinimapBlip : MonoBehaviour
    {
        public void SetTeam(Team t)
        {
            var r = GetComponent<Renderer>();
            if (r != null)
                r.material = MatUtil.Unlit(t == Team.Player
                    ? new Color(0.3f, 0.75f, 1f)
                    : new Color(1f, 0.25f, 0.2f));
        }
    }
}
