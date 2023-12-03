using Godot;
using System.Collections.Generic;
using System;
using System.Linq;
using EditorTools;
using AxialCS;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace Model
{
    /// <summary>
    /// The Model handles the data of the game without any concern/coupling with the representation of that data.
    /// </summary>
    /// <remarks>This script contains unnecessary multithreading, GD.Print, and thread pauses designed to DEMO this script</remarks>
    public class Model_DEMO
    {
        //---------------------
        //----- VARIABLES -----
        //---------------------

        #region VARIABLES

        // STATIC
        private static readonly int DECK_COUNT = 20;
        private static readonly int HAND_START_COUNT = 4;
        private static readonly int CARDS_DRAWN_PER_TURN = 1;
        private static readonly int PLAYER_COUNT = 2;
        private static readonly int BOARD_RADIUS = 2;

        // CARD SET
        private static readonly Card[] CardSet = new Card[] {
            new Card("Base Test", Card.CardType.Base, 10),
            new Card("Resource Test", Card.CardType.Resource, 3),
            new Card("Offense Test", 2, 3, 1)
        };

        private static readonly Card[] CardSet_NoBases = CardSet
            .Where(card => card.TYPE != Card.CardType.Base)
            .ToArray();


        // INSTANCE
        Random random = new Random();

        #region  SESSION DATA       // Data scoped to a session instance

        private int _roundCounter = 0;

        private Card[][] Decks = new Card[PLAYER_COUNT][];
        private Card[] _userDeck
        {
            get
            {
                return Decks[0];
            }
            set
            {
                Decks[0] = value;
            }
        }
        private Card[] _enemyDeck
        {
            get
            {
                return Decks[1];
            }
            set
            {
                Decks[1] = value;
            }
        }

        #endregion

        #region ROUND DATA          // Data scoped to a round instance
        
        private Axial[] base_locations = new Axial[] {
            Axial.Direction(Axial.Cardinal.SW),
            Axial.Direction(Axial.Cardinal.NE)
        };

        private int _turnCounter = 0;
        private int[] CardsDrawn = new int[PLAYER_COUNT];       // How many cards each player has drawn

        // Hands data
        private CardInst[][] Hands = new CardInst[PLAYER_COUNT][];
        private CardInst[] _userHand
        {
            get
            {
                return Hands[0];
            }
            set
            {
                Hands[0] = value;
            }
        }
        private CardInst[] _enemyHand
        {
            get
            {
                return Hands[1];
            }
            set
            {
                Hands[1] = value;
            }
        }

        // Board Data
        private AxialGrid Board;
        private Dictionary<Axial, CardInst>[] PlayersActiveBoards = new Dictionary<Axial, CardInst>[PLAYER_COUNT];
        private Dictionary<Axial, CardInst> AllActiveBoards => PlayersActiveBoards.Where(board => board != null).SelectMany(board => board).GroupBy(pair => pair.Key)
        .ToDictionary(group => group.Key, group => group.First().Value);

        #endregion

        #region  TURN DATA          // Data scoped to a turn instance

        #endregion

        #endregion

        //--------------------------
        //----- INITIALIZATION -----
        //--------------------------

        #region INITIALIZATION
        
        // CONSTRUCTOR
        public Model_DEMO(){
            Task.Run(() => {
                DisplayCardSet();
                StartGame();
            });
        }

        private void DisplayCardSet(){
            GD.Print("----- CARD SET W/ BASES -----");

            for (int i = 0; i < CardSet.Length; i++)
            {
                Card card = CardSet[i];
                GD.Print($"[{i}] = {card}");
            }

            GD.Print("-----------------------------");

            Thread.Sleep(1000);
            
            GD.Print("----- CARD SET W/O BASES -----");

            for (int i = 0; i < CardSet_NoBases.Length; i++)
            {
                Card card = CardSet_NoBases[i];
                GD.Print($"[{i}] = {card}");
            }
            
            GD.Print("-----------------------------");

            Thread.Sleep(1000);
        }

        private void StartGame(){

            _roundCounter = 0;
            
            InitDecks();

            StartRound(ref _roundCounter);
        }

        private void StartRound(ref int roundCounter)
        {
            GD.Print("-------------------------");
            GD.Print($"----- START ROUND {roundCounter} -----");
            GD.Print("-------------------------");

            _turnCounter = 0;
            Board = new AxialGrid(BOARD_RADIUS);
            for (int i = 0; i < PlayersActiveBoards.Length; i++)
            {
                ref Dictionary<Axial, CardInst> refActiveBoard = ref PlayersActiveBoards[i];
                refActiveBoard = new Dictionary<Axial, CardInst>();
            }

            Thread.Sleep(1000);

            InitHands();
            InitBases();

            while(!IsRoundOver(_turnCounter)){
                Thread.Sleep(10);
                StartTurn(ref _turnCounter);
                _turnCounter++;
            }
        }

        private void InitBases(){

            Thread.Sleep(100);


            for(int i = 0; i < PLAYER_COUNT; i++){
                ref Dictionary<Axial, CardInst> refActiveBoard = ref PlayersActiveBoards[i];
                Card BaseCard = CardSet[0];
                Axial BaseLocation = GetBaseLocation(i);

                TryPlaceCard_FromVoid(i, BaseCard, BaseLocation);
            }
        }

        private Axial GetBaseLocation(int player_index){

            if(PLAYER_COUNT > base_locations.Length){
                GD.PrintErr($"Not enough base locations ({base_locations.Length}) for the amount of players({PLAYER_COUNT})");
                return Axial.Empty;
            }

            return base_locations[player_index] * BOARD_RADIUS;
        }

        private bool TryGetBaseCard(int player_index, out CardInst playerBase)
        {
            Dictionary<Axial, CardInst> ActiveBoard = PlayersActiveBoards[player_index];
            Axial baseLocation = GetBaseLocation(player_index);

            if (ActiveBoard.ContainsKey(baseLocation))
            {
                playerBase = ActiveBoard[GetBaseLocation(player_index)];
                return true;
            }
            else
            {
                GD.PrintErr($"Player {player_index} does not have a card at its base location {baseLocation}");

                playerBase = CardInst.EMPTY;
                return false;
            }
        }

        private bool IsRoundOver(int turnCounter){

            for(int i = 0; i < PLAYER_COUNT; i++){
                if(TryGetBaseCard(i, out CardInst playerBase)){
                    if(playerBase.hp <= 0){
                        return true;
                    }
                }
            }
            
            return turnCounter >= 10;
        }

        private void StartTurn(ref int turnCounter)
        {
            GD.Print("------------------------");
            GD.Print($"----- START TURN {turnCounter} -----");
            GD.Print("------------------------");

            Thread.Sleep(500);

            int iPlayerIndex = turnCounter % PLAYER_COUNT;

            // 1. Draw a card
            // 2. Place card(s)
            // 3. Move card(s)
            // 4. Attack card(s)
            // 5. End turn

            TryDrawCard(iPlayerIndex);
            Thread.Sleep(500);

            int rand = random.Next(1, Hands[iPlayerIndex].Length);

            for (int i = 0; i < rand; i++)
            {
                if (TryGetOpenTile(out Axial openTile))
                    TryPlaceCard_FromHand(iPlayerIndex, 0, openTile);
                else
                    GD.Print("Could not place card because all tiles are filled");
                Thread.Sleep(500);
            }
        }

        private bool TryGetOpenTile(out Axial Axial)
        {
            foreach (Axial ax in Board.Axials)
            {
                    if (!AllActiveBoards.ContainsKey(ax))
                    {
                        GD.Print($"Found open axial {ax}");
                        Axial = ax;
                        return true;
                    }
            }

            GD.Print($"Could not find open axial. Returning empty.");
            Axial = Axial.Empty;
            return false;
        }

        private void InitDecks(){
            for (int i = 0; i < PLAYER_COUNT; i++)
            {
                GD.Print($"----- Initializing deck[{i}] -----");
                Thread.Sleep(500);

                ref Card[] refDeck = ref Decks[i];
                refDeck = new Card[DECK_COUNT];

                for(int j = 0; j < refDeck.Length; j++){
                    int rand = random.Next(0,CardSet_NoBases.Length);
                    refDeck[j] = CardSet_NoBases[rand];
                    GD.Print($"Deck [{i}][{j}] : {refDeck[j]}");
                }
                
                GD.Print($"----- Initialized deck[{i}] -----");
                GD.Print("-------------------------------");
            }
        }

        private void InitHands()
        {
            for (int i = 0; i < PLAYER_COUNT; i++)
            {
                GD.Print($"----- Initializing hand[{i}] -----");
                Thread.Sleep(500);

                ref CardInst[] refHand = ref Hands[i];
                ref Card[] refDeck = ref Decks[i];

                refHand = null;

                

                for (int j = 0; j < HAND_START_COUNT; j++)
                {
                    TryDrawCard(i);
                    Thread.Sleep(150);
                }

                DisplayHand(i);

                GD.Print($"----- Initialized hand[{i}] -----");
                GD.Print("-------------------------------");
            }
        }

        private void DisplayHand(int player_index){

            Thread.Sleep(100);

                GD.Print(new string('-', 28));
                GD.Print($"----- Displaying hand[{player_index}] -----");

                ref CardInst[] refHand = ref Hands[player_index];

                for (int i = 0; i < refHand.Length; i++)
                {
                    GD.Print($"[{i}] : {refHand[i].name}");
                }

                GD.Print(new string('-', 28));
        }

        private bool TryDrawCard(int player_index){
            Card[] iDeck = Decks[player_index];
            ref CardInst[] refHand = ref Hands[player_index];
            int deckCount = iDeck.Length;
            ref int refDrawnCount = ref CardsDrawn[player_index]; 

            if(refDrawnCount < deckCount){
                Card drawnCard = iDeck[refDrawnCount];

                int heldCount = AddCardToHand(player_index, drawnCard);

                refDrawnCount++;

                GD.Print($"Player {player_index} drew a card ({drawnCard.NAME}), increasing their hand to {heldCount}. Their drawn count has incremented to {refDrawnCount}");
                return true;
            }
            else{
                GD.Print($"Player {player_index} cannot draw any more cards because their drawn count ({refDrawnCount}) is equal to their deck count ({deckCount})");
                return false;
            }
        }

        /// <summary>
        /// Add a card to a player's hand
        /// </summary>
        /// <param name="player_index">Player's hand</param>
        /// <param name="card">Card to add</param>
        /// <returns>Amount of cards in hand</returns>
        private int AddCardToHand(int player_index, Card card)
        {
            ref CardInst[] refHand = ref Hands[player_index];

            CardInst newCard = new CardInst(card, (ushort)random.Next(ushort.MinValue, ushort.MaxValue));

            if (refHand != null)
            {
                CardInst[] newHand = new CardInst[refHand.Length + 1];
                refHand.CopyTo(newHand, 0);
                newHand[newHand.Length - 1] = newCard;
                refHand = newHand;
            }
            else
            {
                refHand = new CardInst[1] { newCard };
            }

            return refHand.Length;
        }

        private CardInst RemoveCardFromHand(int player_index, int card_index)
        {
            ref CardInst[] refHand = ref Hands[player_index];

            //If index is within bounds,
            if (card_index < refHand.Length && card_index >= 0)
            {
                //Get reference
                CardInst card = refHand[card_index];

                //Remove from hand
                CardInst[] newHand = new CardInst[refHand.Length - 1];
                for (int i = 0, j = 0; i < refHand.Length; i++)
                {
                    if (i != card_index)
                    {
                        newHand[j++] = refHand[i];
                    }
                }
                refHand = newHand;

                GD.Print($"Removed {card.name} (hash:{card.id}) from player {player_index}'s hand");

                //Return removed card
                return card;
            }
            else{
                GD.PrintErr($"Cannot remove index {card_index} from hand[{player_index}] because it is not within the bounds of the hand.");
                return CardInst.EMPTY;
            }
        }

        /// <summary>
        /// Try to place a card from a hand at a location on the board
        /// </summary>
        /// <param name="player_index">Player placing the card (needed to get hand)</param>
        /// <param name="card_index">Index of the card (within the hand)</param>
        /// <param name="location">Axial placement</param>
        /// <returns>True if the card is placed, false if not</returns>
        /// <remarks>Purpose of passing indexes rather than arrays/cards is to manage references and minimize parameter data</remarks>
        private bool TryPlaceCard_FromHand(int player_index, int card_index, Axial location)
        {
            ref CardInst[] refHand = ref Hands[player_index];

            if(IsPlacementLocationValid(location)){
                CardInst card = RemoveCardFromHand(player_index, card_index);
                PlaceCard(player_index, card, location);
                return true;
            }
            else
                return false;
        }

        private bool TryPlaceCard_FromVoid(int player_index, Card card, Axial location){

            if(IsPlacementLocationValid(location)){
                CardInst cardInst = new CardInst(card, (ushort)random.Next(ushort.MinValue, ushort.MaxValue));
                PlaceCard(player_index, cardInst, location);
                return true;
            }
            else
                return false;
        }

        private void PlaceCard(int player_index, CardInst card, Axial location)
        {
            ref Dictionary<Axial, CardInst> refActiveBoard = ref PlayersActiveBoards[player_index];
            refActiveBoard.Add(location, card);

            GD.Print($"Player {player_index} placing card {card.name} (hash:{card.id}) at location {location}");
        }

        private bool IsPlacementLocationValid(Axial location)
        {
            // Check if the location exists on the board
            if (!Board.IsAxialOnGrid(location))
            {
                GD.Print($"Cannot place card at {location} because it does not exist on the board.");
                return false;
            }

            // Check each player's active board to see if a card has already been placed at this location
            foreach (Dictionary<Axial, CardInst> ActiveBoard in PlayersActiveBoards)
            {
                if (ActiveBoard.ContainsKey(location))
                {
                    GD.Print($"Cannot place card at {location} because a card is already placed there.");
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}