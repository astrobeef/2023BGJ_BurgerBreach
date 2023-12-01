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
            .Where(card => card._type != Card.CardType.Base)
            .ToArray();


        // INSTANCE
        private Card[][] Decks = new Card[PLAYER_COUNT][];
        private Card[] _userDeck{
            get {
                return Decks[0];
            }
            set{
                Decks[0] = value;
            }
        }
        private Card[] _enemyDeck{
            get {
                return Decks[1];
            }
            set{
                Decks[1] = value;
            }
        }

        private Card[][] Hands = new Card[PLAYER_COUNT][];
        private Card[] _userHand{
            get {
                return Hands[0];
            }
            set{
                Hands[0] = value;
            }
        }
        private Card[] _enemyHand{
            get {
                return Hands[1];
            }
            set{
                Hands[1] = value;
            }
        }

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

        private async void DisplayCardSet(){
            GD.Print("----- CARD SET W/ BASES -----");

            Task task = new Task(() => {
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
            });

            task.RunSynchronously();
            await task;
            return;
        }

        private void StartRound(){
            GD.Print("-------------------------");
            GD.Print("----- START ROUND 1 -----");
            GD.Print("-------------------------");

                Thread.Sleep(1000);

                InitDeck();
        }

        private void InitDeck(){
            Random random = new Random();

            for (int i = 0; i < Decks.Length; i++)
            {
                GD.Print($"----- Initializing deck[{i}] -----");
                Thread.Sleep(500);


                ref Card[] refDeck = ref Decks[i];
                refDeck = new Card[DECK_COUNT];

                for(int j = 0; j < refDeck.Length; j++){
                    int rand = random.Next(0,CardSet_NoBases.Length);
                    refDeck[j] = CardSet_NoBases[rand];
                    GD.Print($"Deck [{i}][{j}] : {refDeck[j]}");
                Thread.Sleep(150);
                }
                
                GD.Print($"----- Initialized deck[{i}] -----");
                GD.Print("-------------------------------");
            }
        }

        #endregion
    }

    internal struct Card{

        internal string _name;

        public enum CardType {Base, Resource, Offense};
        internal CardType _type;

        internal int _hp;
        internal int _move;
        internal int _atk;

        public Card (string name, CardType type, int hp){
            _name = name;
            _type = type;
            _hp = hp;

            _move = 0;
            _atk = 0;
        }

        public Card (string name, int hp, int move, int atk){
            _name = name;
            _type = CardType.Offense;

            _hp = hp;
            _move = move;
            _atk = atk;            
        }

        public override string ToString()
        {
            return $"({_name}, {_type}, HP:{_hp}, MOVE:{_move}, ATK:{_atk})";
        }
    }
}