using UnityEngine;
using TcgEngine;
using TcgEngine.Client;
using TcgEngine.UI;

namespace CCG
{
    /// <summary>
    /// Fills the kit PlayerUI avatar frames with each player's HERO art so the
    /// corner portraits + HP readouts identify both sides at a glance.
    /// </summary>
    public class CCGPortraits : MonoBehaviour
    {
        private float timer = 0f;

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f)
                return;
            timer = 1f;

            GameClient client = GameClient.Get();
            Game data = client != null ? client.GetGameData() : null;
            if (data == null)
                return;

            foreach (PlayerUI pui in Object.FindObjectsByType<PlayerUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                AvatarUI avatar = pui.GetComponentInChildren<AvatarUI>(true);
                if (avatar == null)
                    continue;
                //PlayerUI knows if it is the local player's panel via its own setup;
                //match by the HP it displays? Simpler: use its serialized is_opponent
                //flag if present, else assign by hierarchy name.
                bool opponent = pui.gameObject.name.Contains("2");
                int pid = opponent ? client.GetOpponentPlayerID() : client.GetPlayerID();
                Player p = data.GetPlayer(pid);
                if (p != null && p.hero != null && p.hero.CardData.art_full != null)
                    avatar.SetImage(p.hero.CardData.art_full);
            }
        }
    }
}
