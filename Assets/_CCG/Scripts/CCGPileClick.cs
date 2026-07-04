using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TcgEngine;
using TcgEngine.Client;
using TcgEngine.UI;

namespace CCG
{
    /// <summary>
    /// Click a DECK or DISCARD pile to browse its cards in the kit's CardSelector
    /// panel (read-only; close with X).
    /// </summary>
    public class CCGPileClick : MonoBehaviour
    {
        private int player_id;
        private bool deck;

        public void Setup(int owner_id, bool is_deck, Canvas canvas, Image raycast_img)
        {
            player_id = owner_id;
            deck = is_deck;

            canvas.worldCamera = Camera.main;
            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            raycast_img.raycastTarget = true;

            EventTrigger trigger = raycast_img.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = raycast_img.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry click = new EventTrigger.Entry();
            click.eventID = EventTriggerType.PointerClick;
            click.callback.AddListener((d) => { OnClick(); });
            trigger.triggers.Add(click);
        }

        private void OnClick()
        {
            GameClient client = GameClient.Get();
            Game data = client != null ? client.GetGameData() : null;
            if (data == null)
                return;
            Player owner = data.GetPlayer(player_id);
            if (owner == null)
                return;

            List<Card> cards = new List<Card>(deck ? owner.cards_deck : owner.cards_discard);
            string title = (deck ? "Deck (" : "Discard (") + cards.Count + ")";
            CardSelector selector = CardSelector.Get();
            if (selector != null)
                selector.Show(cards, title);
        }
    }
}
