using UnityEngine;
using UnityEngine.InputSystem;
using TcgEngine;
using TcgEngine.Client;

namespace CCG
{
    /// <summary>
    /// Right-click a card in your hand to PITCH it: the card goes to the bottom
    /// of your deck and you gain temporary mana equal to its pitch value.
    /// Server-side validation (per-turn limit, your-turn-only) is handled by the
    /// ccg_pitch ability's conditions; this is only the input affordance.
    /// Lives on a GameObject in the game scene.
    /// </summary>
    public class CCGPitchControls : MonoBehaviour
    {
        void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.rightButton.wasPressedThisFrame)
                return;

            GameClient client = GameClient.Get();
            if (client == null)
                return;

            HandCard hover = HandCard.GetFocus();
            if (hover == null)
                return;

            Card card = hover.GetCard();
            if (card == null)
                return;

            AbilityData pitch = AbilityData.Get("ccg_pitch");
            if (pitch == null)
                return;

            //Only meaningful for cards that have the ability (all non-heroes)
            if (!card.CardData.HasAbility(pitch))
                return;

            client.CastAbility(card, pitch);
        }
    }
}
