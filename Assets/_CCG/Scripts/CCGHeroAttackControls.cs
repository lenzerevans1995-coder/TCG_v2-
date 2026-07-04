using UnityEngine;
using TcgEngine;
using TcgEngine.Client;
using TcgEngine.UI;

namespace CCG
{
    /// <summary>
    /// Physics-raycast input for the zone world cards + hero attacks.
    /// Hover: drives CCGZoneCardHover (outline + big preview).
    /// Attack: DRAG from your hero to a target (enemy minion or enemy hero) like
    /// minions do — a teal line follows the drag. Click-arm + click-target also
    /// works. Right-click cancels. Server CCGGameLogic pays the weapon swing cost.
    /// Zone card colliders live on layer 2 (Ignore Raycast) so they never block
    /// the kit's own board raycasts.
    /// </summary>
    public class CCGHeroAttackControls : MonoBehaviour
    {
        private const int ZONE_MASK = 1 << 2; //Ignore Raycast layer

        private static Card armed_hero;
        public static bool IsArmed(Card c) { return armed_hero != null && c != null && armed_hero.uid == c.uid; }
        public static bool IsArmedAny { get { return armed_hero != null; } }

        //Docked-HUD entry points: the hero lives in a screen-space cluster
        //panel now (CCGMatchHUD), so arming and face-swings come from UI
        //clicks; armed targeting of enemy MINIONS still resolves in Update
        //via the kit's BoardCard hover.
        public static void UIToggleArm()
        {
            GameClient client = GameClient.Get();
            Game data = client != null ? client.GetGameData() : null;
            if (data == null || data.state != GameState.Play)
                return;
            if (armed_hero != null) { armed_hero = null; return; } //toggle off
            Player me = data.GetPlayer(client.GetPlayerID());
            if (me == null || me.hero == null)
                return;
            if (data.current_player != client.GetPlayerID()) { WarningText.ShowNotYourTurn(); return; }
            if (me.hero.exhausted) { WarningText.ShowExhausted(); return; }
            Card weapon = data.GetEquipCard(me.hero.equipped_uid);
            if (weapon == null || weapon.GetAttack() <= 0)
                return;
            if (me.mana < weapon.GetMana()) { WarningText.ShowNoMana(); return; }
            armed_hero = me.hero;
        }

        /// <summary>Press on your cluster portrait: arm and start a DRAG with
        /// the teal indication line (same feel as minion attacks). Release
        /// resolves in Update — enemy portrait, enemy minion, or back on your
        /// own portrait to stay armed (click-click mode).</summary>
        public static void UIBeginDrag()
        {
            if (inst == null)
                return;
            if (armed_hero == null)
                UIToggleArm();
            if (armed_hero == null)
                return; //arm failed (not your turn / exhausted / no weapon / no mana)
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null || Camera.main == null)
                return;
            inst.dragging = true;
            inst.drag_from = inst.MouseOnTable(mouse.position.ReadValue()) + Vector3.up * 0.3f;
        }

        public static void UIAttackEnemyHero()
        {
            GameClient client = GameClient.Get();
            Game data = client != null ? client.GetGameData() : null;
            if (data == null || armed_hero == null)
                return;
            Player opp = data.GetOpponentPlayer(client.GetPlayerID());
            if (opp != null)
                client.AttackPlayer(armed_hero, opp);
            armed_hero = null;
        }

        private bool dragging = false;
        private Vector3 drag_from;
        private LineRenderer line;

        private static CCGHeroAttackControls inst;

        void Awake()
        {
            inst = this;
        }

        void Update()
        {
            GameClient client = GameClient.Get();
            Game data = client != null ? client.GetGameData() : null;
            if (data == null || data.state != GameState.Play)
            {
                armed_hero = null;
                dragging = false;
                SetLine(false, Vector3.zero, Vector3.zero);
                CCGZoneCardHover.SetHoverInstance(null);
                return;
            }

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null || Camera.main == null)
                return;

            //UI has PRIORITY: while the pointer is over any UI (hand cards,
            //mulligan/OK overlays, buttons), zone cards neither hover nor take
            //clicks — physics hover was stealing input through those layers.
            //Mid-drag is exempt so an attack drag can end anywhere.
            var esys = UnityEngine.EventSystems.EventSystem.current;
            if (!dragging && esys != null && esys.IsPointerOverGameObject())
            {
                CCGZoneCardHover.SetHoverInstance(null);
                return;
            }

            Vector2 mpos = mouse.position.ReadValue();
            CCGZoneCardHover hover = RaycastZoneCard(mpos);
            CCGZoneCardHover.SetHoverInstance(hover);

            if (armed_hero != null && data.current_player != client.GetPlayerID())
            {
                armed_hero = null;
                dragging = false;
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                armed_hero = null;
                dragging = false;
            }

            int my_id = client.GetPlayerID();
            Player me = data.GetPlayer(my_id);
            Card hover_card = hover != null ? hover.GetCard() : null;

            //PRESS on my hero: arm + start drag
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (hover_card != null && me != null && me.hero != null && hover_card.uid == me.hero.uid)
                {
                    if (TryArm(client, data, me))
                    {
                        dragging = true;
                        drag_from = hover.transform.position + Vector3.up * 0.3f;
                    }
                }
                else if (armed_hero != null && !dragging)
                {
                    //Click-click mode: armed, clicked a target (or empty = disarm).
                    //(!dragging: a drag freshly started from the HUD portrait this
                    //frame must not instantly resolve/disarm here)
                    ResolveTargetAt(client, data, my_id, hover_card);
                }
            }

            //RELEASE after dragging: resolve the target under the cursor
            if (dragging && mouse.leftButton.wasReleasedThisFrame)
            {
                dragging = false;
                //Released still on the hero (world card or HUD portrait): stay armed (click-click mode)
                bool on_own_hero = (hover_card != null && me != null && me.hero != null && hover_card.uid == me.hero.uid)
                    || CCGMatchHUD.IsPointerOverMyPortrait;
                if (!on_own_hero)
                    ResolveTargetAt(client, data, my_id, hover_card);
            }

            //Drag line
            if (dragging && armed_hero != null)
            {
                Vector3 end = MouseOnTable(mpos);
                SetLine(true, drag_from, end);
            }
            else
            {
                SetLine(false, Vector3.zero, Vector3.zero);
            }
        }

        private bool TryArm(GameClient client, Game data, Player me)
        {
            if (armed_hero != null) { armed_hero = null; return false; } //toggle off
            if (data.current_player != client.GetPlayerID()) { WarningText.ShowNotYourTurn(); return false; }
            if (me.hero.exhausted) { WarningText.ShowExhausted(); return false; }
            Card weapon = data.GetEquipCard(me.hero.equipped_uid);
            if (weapon == null || weapon.GetAttack() <= 0) return false;
            if (me.mana < weapon.GetMana()) { WarningText.ShowNoMana(); return false; }
            armed_hero = me.hero;
            return true;
        }

        private void ResolveTargetAt(GameClient client, Game data, int my_id, Card zone_card)
        {
            if (armed_hero == null)
                return;

            //Released on the docked enemy portrait → swing at their face
            if (CCGMatchHUD.IsPointerOverEnemyPortrait)
            {
                client.AttackPlayer(armed_hero, data.GetOpponentPlayer(my_id));
                armed_hero = null;
                return;
            }

            //Enemy hero zone card → swing at the player
            if (zone_card != null && zone_card.CardData.type == CardType.Hero && zone_card.player_id != my_id)
            {
                client.AttackPlayer(armed_hero, data.GetPlayer(zone_card.player_id));
                armed_hero = null;
                return;
            }

            //Enemy board minion under the cursor (kit hover focus)
            BoardCard bcard = BoardCard.GetFocus();
            Card target = bcard != null ? bcard.GetCard() : null;
            if (target != null && target.player_id != my_id)
            {
                client.AttackTarget(armed_hero, target);
                armed_hero = null;
                return;
            }

            //Nothing valid: disarm
            armed_hero = null;
        }

        private CCGZoneCardHover RaycastZoneCard(Vector2 screen_pos)
        {
            Ray ray = Camera.main.ScreenPointToRay(screen_pos);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f, ZONE_MASK);
            CCGZoneCardHover best = null;
            float best_dist = float.MaxValue;
            foreach (RaycastHit hit in hits)
            {
                CCGZoneCardHover h = hit.collider.GetComponent<CCGZoneCardHover>();
                if (h != null && hit.distance < best_dist)
                {
                    best = h;
                    best_dist = hit.distance;
                }
            }
            return best;
        }

        private Vector3 MouseOnTable(Vector2 screen_pos)
        {
            Ray ray = Camera.main.ScreenPointToRay(screen_pos);
            Plane table = new Plane(Vector3.up, new Vector3(0f, 0.3f, 0f));
            float d;
            if (table.Raycast(ray, out d))
                return ray.GetPoint(d);
            return drag_from;
        }

        private void SetLine(bool visible, Vector3 from, Vector3 to)
        {
            if (line == null)
            {
                if (!visible) return;
                GameObject lgo = new GameObject("CCG_HeroAttackLine");
                line = lgo.AddComponent<LineRenderer>();
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startWidth = 0.09f;
                line.endWidth = 0.05f;
                line.positionCount = 2;
                //Draw ABOVE the HUD canvas: the indication line was rendering
                //underneath the docked cluster panels
                line.sortingOrder = 500;
                Color teal = new Color(0.3f, 1f, 0.9f, 0.9f);
                line.startColor = teal;
                line.endColor = teal;
            }
            line.enabled = visible;
            if (visible)
            {
                line.SetPosition(0, from);
                line.SetPosition(1, to);
            }
        }
    }
}
