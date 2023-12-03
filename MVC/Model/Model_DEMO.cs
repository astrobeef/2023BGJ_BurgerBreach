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

        private static readonly int COLLISION_DAMAGE = 1;

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
        private CardInst[] ActiveBoard = new CardInst[0];

        #endregion

        #region  TURN DATA          // Data scoped to a turn instance

        /// <summary>
        /// The index of the player whose turn it is
        /// </summary>
        private int turnPlayerIndex = 0;



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
            ActiveBoard = new CardInst[0];

            Thread.Sleep(1000);

            InitHands();
            InitBases();

            while(!IsRoundOver(_turnCounter)){
                Thread.Sleep(100);
                StartTurn(ref _turnCounter);
                _turnCounter++;
            }
        }

        private void InitBases(){

            Thread.Sleep(100);

            for(int i = 0; i < PLAYER_COUNT; i++){
                Card BaseCard = CardSet[0];
                Axial BaseLocation = CardSet_GetBaseLocation(i);

                TryPlaceCard_FromVoid(i, BaseLocation, BaseCard);
            }
        }

        private Axial CardSet_GetBaseLocation(int player_index){

            if(PLAYER_COUNT > base_locations.Length){
                GD.PrintErr($"Not enough base locations ({base_locations.Length}) for the amount of players({PLAYER_COUNT})");
                return Axial.Empty;
            }

            return base_locations[player_index] * BOARD_RADIUS;
        }

        private bool ActiveBoard_TryGetBaseCard(int player_index, out CardInst playerBase)
        {
            Axial baseLocation = CardSet_GetBaseLocation(player_index);

            if (ActiveBoard_ContainsAxial(baseLocation, out int baseIndex))
            {
                playerBase = ActiveBoard[baseIndex];
                return true;
            }
            else
            {
                GD.PrintErr($"Player {player_index} does not have a card at its base location {baseLocation}");

                playerBase = CardInst.EMPTY;
                return false;
            }
        }

        private bool ActiveBoard_ContainsAxial(Axial axial, out int index)
        {
            for (int i = 0; i < ActiveBoard.Length; i++)
            {
                CardInst card = ActiveBoard[i];
                if (card.pos == axial){
                    index = i;
                    return true;
                }
            }
            
            index = -1;
            return false;
        }

        private bool IsRoundOver(int turnCounter){

            for(int i = 0; i < PLAYER_COUNT; i++){
                if(ActiveBoard_TryGetBaseCard(i, out CardInst playerBase)){
                    if(playerBase.hp <= 0){
                        return true;
                    }
                }
            }
            
            return turnCounter >= 100;
        }

        private void StartTurn(ref int turnCounter)
        {
            GD.Print("------------------------");
            GD.Print($"----- START TURN {turnCounter} -----");
            GD.Print("------------------------");

            Thread.Sleep(500);

            turnPlayerIndex = turnCounter % PLAYER_COUNT;
            InitActiveCards();

            // 1. Draw a card

            TryDrawCard(turnPlayerIndex);

            Thread.Sleep(500);
            
            // 2. Place card(s)

            int rand = random.Next(1, Hands[turnPlayerIndex].Length);

            for (int i = 0; i < rand; i++)
            {
                if (TryGetOpenTile(out Axial openTile))
                    TryPlaceCard_FromHand(turnPlayerIndex, 0, openTile);
                else
                    GD.Print("Could not place card because all tiles are filled");
                Thread.Sleep(500);
            }

            Thread.Sleep(500);

            // 3. Move card(s)

            GD.Print("--- Beginning Movement ---");

            foreach (CardInst occupant in ActiveBoard)
            {
                if (occupant.ownerIndex == turnPlayerIndex)
                {
                    CardInst iCard = occupant;

                    Axial oldPos = iCard.pos;

                    GD.Print($"Player {turnPlayerIndex} attempting to move {iCard.name} at {oldPos}.");

                    if (CardInst_TryRandomMoveCard(iCard, out Axial newPos))
                    {
                        GD.Print($"Player {turnPlayerIndex} moved {iCard.name} (hash:{iCard.id}) from {oldPos} to {newPos}.");
                    }
                    else{
                        GD.Print($"Player {turnPlayerIndex} could not move {iCard.name}");
                    }
                }
            }

            Thread.Sleep(500);

            GD.Print("--- Beginning Combat ---");

            foreach (CardInst occupant in ActiveBoard)
            {
                if (occupant.ownerIndex == turnPlayerIndex)
                {
                    CardInst iCard = occupant;

                    GD.Print($"Player {turnPlayerIndex} attempting to attack with {iCard.name} at {iCard.pos}.");

                    if (CardInst_TryRandomAttack(iCard))
                    {
                        GD.Print($"Player {turnPlayerIndex} made an attack with {iCard.name} (hash:{iCard.id}) from {iCard.pos}.");
                    }
                    else{
                        GD.Print($"Player {turnPlayerIndex} could not move {iCard.name}");
                    }
                }
            }

            // 4. Attack card(s)
            // 5. End turn
        }

        private bool CardInst_TryRandomMoveCard(CardInst card, out Axial newPos){

            Axial initPos = card.pos;
            newPos = initPos;

            int iDirection = 0;

            while(card.CanMove(out int remainingMovement)){
                if(iDirection >= Axial.CARDINAL_LENGTH)
                {
                    GD.Print($"Player {card.ownerIndex} can't move this unit anymore because there are no valid directions to move in.");
                    return (newPos != initPos);
                }

                Axial randDirection = Axial.Direction((Axial.Cardinal)iDirection);
                Axial movePosition = card.pos + randDirection;
                
                if(CardInst_TryMove(true, card, movePosition)){
                    newPos = movePosition;
                }

                iDirection++;
            }

            return (newPos != initPos);
        }

        private bool CardInst_TryMove(bool isWillful, CardInst card, Axial newPos)
        {
            return CardInst_TryMove(isWillful, card, newPos, out CardInst dummyOccupant);
        }

        private bool CardInst_TryMove(bool isWillful, CardInst card, Axial newPos, out CardInst occupant)
        {
                if(IsPlacementLocationValid(newPos, out occupant)){
                    card.Move(isWillful, newPos);
                    return true;
                }
                else{
                    return false;
                }
        }

        private bool CardInst_TryRandomAttack(CardInst card)
        {
            int iDirection = 0;

            while(card.CanAttack())
            {
                if(iDirection >= Axial.CARDINAL_LENGTH)
                {
                    GD.Print($"Player {card.ownerIndex} can't attack with this unit anymore because there are no valid targets.");
                    return false;
                }
                
                Axial randDirection = Axial.Direction((Axial.Cardinal)iDirection);
                Axial attackPosition = card.pos + randDirection;

                if(ActiveBoard_ContainsAxial(attackPosition, out int targetIndex)){
                    CardInst target = ActiveBoard[targetIndex];

                    // If this target is not on the same team
                    if(target.ownerIndex != card.ownerIndex){
                        GD.Print($"Player {card.ownerIndex} attacked {target.name} @ {target.pos}.");

                        card.Attack();

                        if (!target.Damage(card.atk))
                        {
                            if(target.type == Card.CardType.Base){
                                GD.PrintErr($"Player {target.ownerIndex} has had their base destroyed! Need to fire an event to end the game.");
                                return true;
                            }
                            else{
                                ActiveBoard_RemoveCard(target);
                            }
                        }
                        else if (target.type == Card.CardType.Offense){
                            Axial attackDisplacement = target.pos + randDirection;
                            if(CardInst_TryMove(false, target, attackDisplacement, out CardInst occupant))
                            {
                                GD.Print($"The attack successfully displaced the target.");
                            }
                            else if(occupant != CardInst.EMPTY)
                            {
                                GD.Print($"The target could not be displaced because it collided with {occupant}. Damaging both units.");
                                target.Damage(COLLISION_DAMAGE);
                                occupant.Damage(COLLISION_DAMAGE);
                            }
                            else{
                                GD.Print($"The target could not be displaced because it would have moved off the map. No consequences.");
                            }
                        }

                        return true;
                    }
                }

                iDirection++;
            }

            GD.Print($"Player {card.ownerIndex} can't attack with this unit.");
            return false;
        }

        private bool TryGetOpenTile(out Axial Axial)
        {
            foreach (Axial ax in Board.Axials)
            {
                    if (!ActiveBoard_ContainsAxial(ax, out int dummyIndex))
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

                ref Card[] refHand = ref Hands[i];
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

        private void InitActiveCards()
        {
            foreach(CardInst card in ActiveBoard){
                card.ResetTurnActions();
            }
        }

        private void DisplayHand(int player_index){

            Thread.Sleep(100);

                GD.Print(new string('-', 28));
                GD.Print($"----- Displaying hand[{player_index}] -----");

                ref Card[] refHand = ref Hands[player_index];

                for (int i = 0; i < refHand.Length; i++)
                {
                    GD.Print($"[{i}] : {refHand[i].NAME}");
                }

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
        /// <param name="newCard">Card to add</param>
        /// <returns>Amount of cards in hand</returns>
        private int AddCardToHand(int player_index, Card newCard)
        {
            ref Card[] refHand = ref Hands[player_index];

            if (refHand != null)
            {
                Card[] newHand = new Card[refHand.Length + 1];
                refHand.CopyTo(newHand, 0);
                newHand[newHand.Length - 1] = newCard;
                refHand = newHand;
            }
            else
            {
                refHand = new Card[1] { newCard };
            }

            return refHand.Length;
        }

        private Card RemoveCardFromHand(int player_index, int card_index)
        {
            ref Card[] refHand = ref Hands[player_index];

            //If index is within bounds,
            if (card_index < refHand.Length && card_index >= 0)
            {
                //Get reference
                Card card = refHand[card_index];

                //Remove from hand
                Card[] newHand = new Card[refHand.Length - 1];
                for (int i = 0, j = 0; i < refHand.Length; i++)
                {
                    if (i != card_index)
                    {
                        newHand[j++] = refHand[i];
                    }
                }
                refHand = newHand;

                GD.Print($"Removed {card.NAME} from player {player_index}'s hand");

                //Return removed card
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
        private bool TryPlaceCard_FromHand(int player_index, int card_index, Axial location)
        {
            ref Card[] refHand = ref Hands[player_index];

            if(IsPlacementLocationValid(location)){
                Card card = RemoveCardFromHand(player_index, card_index);
                CardInst cardInst = new CardInst(player_index, location, card);

                ActiveBoard_AddCard(cardInst);

                return true;
            }
            else
                return false;
        }

        private bool TryPlaceCard_FromVoid(int player_index, Axial location, Card card){

            if(IsPlacementLocationValid(location)){
                CardInst cardInst = new CardInst(player_index, location, card);

                ActiveBoard_AddCard(cardInst);

                return true;
            }
            else
                return false;
        }

        private void ActiveBoard_AddCard(CardInst newCard)
        {
            if (ActiveBoard != null)
            {
                CardInst[] newActiveBoard = new CardInst[ActiveBoard.Length + 1];
                ActiveBoard.CopyTo(newActiveBoard, 0);
                newActiveBoard[newActiveBoard.Length - 1] = newCard;

                ActiveBoard = newActiveBoard;
            }
            else
            {
                ActiveBoard = new CardInst[1] { newCard };
            }

            GD.Print($"Player {newCard.ownerIndex} placing card {newCard.name} (hash:{newCard.id}) at location {newCard.pos}");
        }

        private void ActiveBoard_RemoveCard(CardInst card){

            int card_index = -1;

            for (int i = 0; i < ActiveBoard.Length; i++)
            {
                if(card == ActiveBoard[i]){
                    card_index = i;
                    break;
                }
            }

            if(card_index >= 0){
                ActiveBoard_RemoveCard(card_index);
            }
            else{
                GD.PrintErr($"Cannot remove card {card} because it is not on the board.");
            }
        }

        private void ActiveBoard_RemoveCard(int card_index){

                CardInst[] newActiveBoard = new CardInst[ActiveBoard.Length - 1];
                for (int i = 0, j = 0; i < ActiveBoard.Length; i++)
                {
                    if (i != card_index)
                    {
                        newActiveBoard[j++] = ActiveBoard[i];
                    }
                }

                ActiveBoard = newActiveBoard;
        }

        private bool IsPlacementLocationValid(Axial location)
        {
            return IsPlacementLocationValid(location, out CardInst dummyCard);
        }

        private bool IsPlacementLocationValid(Axial location, out CardInst occupant)
        {
            occupant = CardInst.EMPTY;

            // Check if the location exists on the board
            if (!Board.IsAxialOnGrid(location))
            {
                GD.Print($"Cannot place card at {location} because it does not exist on the board.");
                return false;
            }

            // Check active board to see if a card has already been placed at this location
            if (ActiveBoard_ContainsAxial(location, out int boardIndex))
            {
                GD.Print($"Cannot place card at {location} because a card is already placed there.");
                occupant = ActiveBoard[boardIndex];
                return false;
            }

            return true;
        }

        #endregion
    }
}