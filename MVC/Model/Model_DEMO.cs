using Godot;
using System.Collections.Generic;
using System;
using System.Linq;
using EditorTools;
using AxialCS;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection.Metadata;

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
            .Where(card => card.type != Card.CardType.Base)
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
        private Card[][] Hands = new Card[PLAYER_COUNT][];
        private Card[] _userHand
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
        private Card[] _enemyHand
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
        private Dictionary<Axial, Card>[] PlayersActiveBoards = new Dictionary<Axial, Card>[PLAYER_COUNT];

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

            Thread.Sleep(1000);

            InitHands();
            InitBases();

            Board = new AxialGrid(BOARD_RADIUS);

            while(!IsRoundOver(_turnCounter)){
                StartTurn(ref _turnCounter);
                _turnCounter++;
            }
        }

        private void InitBases(){

            if(PLAYER_COUNT > base_locations.Length){
                GD.PrintErr($"Not enough base locations ({base_locations.Length}) for the amount of players({PLAYER_COUNT})");
            }

            for(int i = 0; i < PLAYER_COUNT; i++){
                ref Dictionary<Axial, Card> ActiveBoard = ref PlayersActiveBoards[i];
                Axial BaseLocation = base_locations[i] * BOARD_RADIUS;
                Card BaseCard = CardSet[0];

                TryPlaceCard_FromVoid(i, BaseCard, BaseLocation);

                GD.Print($"Player {i}'s base will be placed at {BaseLocation}");
            }
        }

        private bool IsRoundOver(int turnCounter){
            for(int i = 0; i < PLAYER_COUNT; i++){
                Dictionary<Axial, Card> ActiveBoard = PlayersActiveBoards[i];
                
            }
            
            return turnCounter < 10;
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
                if (TryGetEmptyTile(out Axial openTile))
                    TryPlaceCard_FromHand(iPlayerIndex, 0, openTile);
                else
                    GD.Print("Could not place card because all tiles are filled");
                Thread.Sleep(500);
            }
        }

        private bool TryGetEmptyTile(out Axial Axial)
        {
            foreach (Axial ax in Board.Axials)
            {
                foreach (Dictionary<Axial, Card> ActiveBoard in PlayersActiveBoards)
                {
                    if (!ActiveBoard.ContainsKey(ax))
                    {
                        Axial = ax;
                        return true;
                    }
                }
            }

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

                ref Card[] refHand = ref Hands[i];
                ref Card[] refDeck = ref Decks[i];

                refHand = null;

                

                for (int j = 0; j < HAND_START_COUNT; j++)
                {
                    TryDrawCard(i);
                    Thread.Sleep(150);
                }

                GD.Print($"----- Initialized hand[{i}] -----");
                GD.Print("-------------------------------");

                DisplayHand(i);

            }
        }

        private void DisplayHand(int player_index){

                GD.Print(new string('-', 28));
                GD.Print($"----- Displaying hand[{player_index}] -----");

                ref Card[] refHand = ref Hands[player_index];

                for (int i = 0; i < refHand.Length; i++)
                {
                    GD.Print($"[{i}] : {refHand[i].name}");
                }

                GD.Print(new string('-', 28));
                GD.Print(new string('-', 28));
        }

        private bool TryDrawCard(int player_index){
            Card[] iDeck = Decks[player_index];
            ref Card[] refHand = ref Hands[player_index];
            int deckCount = iDeck.Length;
            ref int refDrawnCount = ref CardsDrawn[player_index]; 

            if(refDrawnCount < deckCount){
                Card drawnCard = iDeck[refDrawnCount];

                int heldCount = AddCardToHand(player_index, drawnCard);

                refDrawnCount++;

                GD.Print($"Player {player_index} drew a card ({drawnCard.name}), increasing their hand to {heldCount}. Their drawn count has incremented to {refDrawnCount}");
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
            ref Card[] refHand = ref Hands[player_index];

            if (refHand != null)
            {
                Card[] newHand = new Card[refHand.Length + 1];
                refHand.CopyTo(newHand, 0);
                newHand[newHand.Length - 1] = card;
                refHand = newHand;
            }
            else
            {
                refHand = new Card[1] { card };
            }

            return refHand.Length;
        }

        private Card RemoveCardFromHand(int player_index, int card_index)
        {
            ref Card[] refHand = ref Hands[player_index];

            if (card_index < refHand.Length && card_index >= 0)
            {
                Card card = refHand[card_index];

                Card[] newHand = new Card[refHand.Length - 1];

                for (int i = 0, j = 0; i < refHand.Length; i++)
                {
                    if (i != card_index)
                    {
                        newHand[j++] = refHand[i];
                    }
                }

                refHand = newHand;
                return card;
            }
            else{
                GD.PrintErr($"Cannot remove index {card_index} from hand[{player_index}] because it is not within the bounds of the hand.");
                return Card.EMPTY;
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
        private bool TryPlaceCard_FromHand(int player_index, int card_index, Axial location){

            ref Card[] refHand = ref Hands[player_index];
            Card card = refHand[card_index];
            
            ref Dictionary<Axial, Card> refActiveBoard = ref PlayersActiveBoards[player_index];

            if(refActiveBoard.ContainsKey(location)){
                GD.Print($"Cannot place card {card.name} because the location {location} is already occupied by {refActiveBoard[location].name}");
                return false;
            }
            else if(Board.IsAxialOnGrid(location))
            {
                RemoveCardFromHand(player_index, card_index);
                refActiveBoard.Add(location, card);
                GD.Print($"Placed card {card.name} at location {location}");
                return true;
            }
            else{
                GD.Print($"Cannot place card {card.name} because the location {location} does not exist on the board.");
                return false;
            }
        }

        private bool TryPlaceCard_FromVoid(int player_index, Card card, Axial location){
            return false;
        }

        #endregion
    }

    internal struct Card{

        internal string name;

        public enum CardType {Base, Resource, Offense};
        internal CardType type;

        internal int hp;
        internal int move;
        internal int atk;

        internal static Card EMPTY = new Card("NULL", CardType.Base, -1);

        public Card (string name, CardType type, int hp){
            this.name = name;
            this.type = type;
            this.hp = hp;

            move = 0;
            atk = 0;
        }

        public Card (string name, int hp, int move, int atk){
            this.name = name;
            type = CardType.Offense;

            this.hp = hp;
            this.move = move;
            this.atk = atk;            
        }

        public override string ToString()
        {
            return $"({name}, {type}, HP:{hp}, MOVE:{move}, ATK:{atk})";
        }

        public static bool operator ==(Card a, Card b){
            return a.name == b.name;
        }
        public static bool operator !=(Card a, Card b){
            return a.name != b.name;
        }
		public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object obj)
		{
			if (obj is Card obj_card)
			{
				return this == obj_card;
			}

			return false;
		}

		public override int GetHashCode()
		{
			// Use a prime number to combine hash codes in a way that reduces the chance of collisions
			const int prime = 31;
			int hash = 17;  // Another prime number
			hash = hash * prime + name.GetHashCode();
			return hash;
		}

    }
}