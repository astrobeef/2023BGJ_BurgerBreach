using Godot;
using System.Collections.Generic;
using System;
using System.Linq;
using EditorTools;
using AxialCS;
using System.Threading.Tasks;
using System.Threading;

namespace Deck{
    public class EDITOR_DeckSpaghetti
    {
        //---------------------
        //----- VARIABLES -----
        //---------------------

        #region VARIABLES

        // PARENT
        public EDITOR_Tool editor;

        // STATIC
        private static readonly int DECK_COUNT = 20;
        private static readonly int HAND_START_COUNT = 4;
        private static readonly int CARDS_DRAWN_PER_TURN = 1;
        private static readonly int PLAYER_COUNT = 2;

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

        #region  DECK DATA

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

        private int[] CardsDrawn = new int[PLAYER_COUNT];       // How many cards each player has drawn

        #endregion

        private int _turnCounter = 0;

        #endregion

        //--------------------------
        //----- INITIALIZATION -----
        //--------------------------

        #region INITIALIZATION
        
        // CONSTRUCTOR
        public EDITOR_DeckSpaghetti(EDITOR_Tool editor){
            this.editor = editor;
            Task.Run(() => {
                DisplayCardSet();
                StartRound();
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

        private void StartRound(){
            GD.Print("-------------------------");
            GD.Print("----- START ROUND 1 -----");
            GD.Print("-------------------------");

                Thread.Sleep(1000);

                InitDecks();
                InitHands();
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
                Card newCard = iDeck[refDrawnCount];

                if(refHand != null){
                    Card[] newHand = new Card[refHand.Length + 1];
                    refHand.CopyTo(newHand, 0);
                    newHand[newHand.Length - 1] = newCard;
                    refHand = newHand;
                }
                else{
                    refHand = new Card[1] {newCard};
                }

                refDrawnCount++;

                GD.Print($"Player {player_index} drew a card ({newCard.name}). Their drawn count has incremented to ({refDrawnCount})");
                return true;
            }
            else{
                GD.Print($"Player {player_index} cannot draw any more cards because their drawn count ({refDrawnCount}) is equal to their deck count ({deckCount})");
                return false;
            }
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