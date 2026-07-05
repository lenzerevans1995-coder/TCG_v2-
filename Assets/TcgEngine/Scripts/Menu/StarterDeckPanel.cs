using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// Selection of your starter deck
    /// Will only appear in the main menu when in API mode with a new account
    /// </summary>

    public class StarterDeckPanel : UIPanel
    {
        public DeckDisplay[] decks;

        public Text error;

        private static StarterDeckPanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        private void RefreshPanel()
        {
            int index = 0;
            foreach (DeckData deck in GameplayData.Get().starter_decks)
            {
                if (index < decks.Length)
                {
                    DeckDisplay display = decks[index];
                    display.SetDeck(deck);
                    index++;
                }
            }

            //CCG-EDIT: hide unused deck slots and their Select buttons (kit prefab
            //has 3 slots, we may have fewer starter decks)
            for (int i = index; i < decks.Length; i++)
            {
                decks[i].Hide();
                Transform btn = decks[i].transform.parent.Find("Select" + i);
                if (btn != null)
                    btn.gameObject.SetActive(false);
            }
        }

        private bool claiming = false; //CCG-EDIT: async claim, block double-click

        private void ChooseDeck(string deck_id)
        {
            if (claiming)
                return;
            claiming = true;
            if (Authenticator.Get().IsTest())
                ChooseDeckTest(deck_id);
            if (Authenticator.Get().IsApi())
                ChooseDeckApi(deck_id);
        }

        private async void ChooseDeckTest(string deck_id)
        {
            UserData udata = Authenticator.Get().UserData;
            DeckData deck = DeckData.Get(deck_id);
            if (deck == null)
                return;

            UserDeckData udeck = new UserDeckData();
            udeck.tid = deck_id + "_" + GameTool.GenerateRandomID(4, 7); //Add random id to differentiate from the starter deck if edited
            udeck.title = deck.title;
            //CROWS-EDIT: champions are deck cards, decks have no hero
            udeck.hero = deck.hero != null ? new UserCardData(deck.hero, VariantData.GetDefault()) : null;

            List<UserCardData> cards = new List<UserCardData>();
            foreach (CardData card in deck.cards)
            {
                UserCardData ucard = new UserCardData(card, VariantData.GetDefault());
                cards.Add(ucard);
            }

            udeck.cards = cards.ToArray();
            udata.AddDeck(udeck);
            udata.AddReward(udeck.tid);

            await Authenticator.Get().SaveUserData();

            CollectionPanel.Get().ReloadUserDecks();
            Hide();
        }

        private async void ChooseDeckApi(string deck_id)
        {
            RewardGainRequest req = new RewardGainRequest();
            req.reward = deck_id;

            if (error != null)
                error.text = "";

            string url = ApiClient.ServerURL + "/users/rewards/gain/" + ApiClient.Get().UserID;
            string json = ApiTool.ToJson(req);
            WebResponse res = await ApiClient.Get().SendPostRequest(url, json);
            if (res.success)
            {
                CollectionPanel.Get().ReloadUserDecks();
                Hide();
            }
            else
            {
                claiming = false; //CCG-EDIT: allow retry after API error
                if (error != null)
                    error.text = res.error;
            }
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            if(error != null)
                error.text = "";
            RefreshPanel();
        }

        public void OnClickDeck(int index)
        {
            if (index < decks.Length)
            {
                DeckDisplay display = decks[index];
                string deck = display.GetDeck();
                ChooseDeck(deck);
            }
        }

        public static StarterDeckPanel Get()
        {
            return instance;
        }
    }
}
