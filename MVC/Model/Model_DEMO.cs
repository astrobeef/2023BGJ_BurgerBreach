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

            if (ActiveBoard_IsAxialOccupied(baseLocation, out int baseIndex))
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

        /// <summary>
        /// 'out' index within <see cref="ActiveBoard"/> of the unit at the axial position, if one exists
        /// </summary>
        /// <returns>True if a unit exists at the parameter axial, false if not</returns>
        private bool ActiveBoard_IsAxialOccupied(Axial axial, out int boardIndex)
        {
            for (int i = 0; i < ActiveBoard.Length; i++)
            {
                CardInst card = ActiveBoard[i];
                if (card.pos == axial){
                    boardIndex = i;
                    return true;
                }
            }
            
            boardIndex = -1;
            return false;
        }

        /// <summary>
        /// 'out' indexes of all adjacent neighbors
        /// </summary>
        /// <returns>True if at least one neighbor found, false if not</returns>
        private bool ActiveBoard_FindNeighbors(Axial axial, out int[] neighborBoardIndexes)
        {
            List<int> neighborBoardIndexes_List = new List<int>(0);

            for (int i = 0; i < Axial.CARDINAL_LENGTH; i++)
            {
                Axial iDirection = Axial.Direction((Axial.Cardinal)i);
                Axial neighbor = axial + iDirection;

                if (ActiveBoard_IsAxialOccupied(neighbor, out int neighborBoardIndex))
                {
                    neighborBoardIndexes_List.Add(neighborBoardIndex);
                }
            }

            neighborBoardIndexes = neighborBoardIndexes_List.ToArray();
            return neighborBoardIndexes.Length > 0;
        }

        /// <summary>
        /// 'out' the first enemy neighbor found
        /// </summary>
        /// <returns>True if enemy found, false if not</returns>
        /// <remarks>Used in <see cref="CardInst_TryRandomAttack"/></remarks>
        private bool ActiveBoard_FindEnemyNeighbor(int ownerIndex, Axial originAxial, out CardInst enemyNeighborUnit)
        {
            int[] neighborBoardIndexes;
            if(ActiveBoard_FindNeighbors(originAxial, out neighborBoardIndexes)){
                foreach(int index in neighborBoardIndexes){
                    CardInst neighborUnit = ActiveBoard[index];
                    if(neighborUnit.ownerIndex != ownerIndex){
                        enemyNeighborUnit = neighborUnit;
                        return true;
                    }
                }
            }

            enemyNeighborUnit = CardInst.EMPTY;
            return false;
        }

        /// <summary>
        /// 'out' the first friendly, non-offense neighbor found
        /// </summary>
        /// <returns>True if friendly, non-offense neighbor found, false if not</returns>
        private bool ActiveBoard_FindFriendlyNonOffenseNeighbor(int ownerIndex, Axial axial, out CardInst friendlyUnit)
        {
            int[] neighborBoardIndexes;
            if(ActiveBoard_FindNeighbors(axial, out neighborBoardIndexes)){
                foreach(int index in neighborBoardIndexes){
                    CardInst neighbor = ActiveBoard[index];
                    if(neighbor.ownerIndex == ownerIndex && neighbor.type != Card.CardType.Offense){
                        friendlyUnit = neighbor;
                        return true;
                    }
                }
            }

            friendlyUnit = CardInst.EMPTY;
            return false;
        }

        private bool ActiveBoard_AllFriendlyUnits(int ownerIndex, out CardInst[] friendlyUnits)
        {
            List<CardInst> friendlyUnits_List = new List<CardInst>(0);

            foreach(CardInst unit in ActiveBoard)
            {
                if(unit.ownerIndex == ownerIndex)
                {
                    friendlyUnits_List.Add(unit);
                }
            }

            friendlyUnits = friendlyUnits_List.ToArray();
            return (friendlyUnits.Length > 0);
        }

        /// <summary>
        /// Get all non-offense friendly units from the active board
        /// </summary>
        /// <param name="ownerIndex"></param>
        /// <param name="friendlyUnits_nonOf">'out' list of non-offense friendly units</param>
        /// <returns>True if at least one unit found, false if not</returns>
        /// <remarks>The 'out' list sorts 'base' cards to the end of the array</remarks>
        private bool ActiveBoard_AllNonOffenseFriendlyUnits(int ownerIndex, out CardInst[] friendlyUnits_nonOf)
        {
            List<CardInst> friendlyUnits_List = new List<CardInst>(0);

            if (ActiveBoard_AllFriendlyUnits(ownerIndex, out CardInst[] friendlyUnits))
            {
                foreach (CardInst unit in friendlyUnits)
                {
                    if (unit.type != Card.CardType.Offense)
                    {
                        friendlyUnits_List.Add(unit);
                    }
                }
            }

            // Separate 'base' cards so that they can be appended to the back (for priority as last pick)
            var baseCards = friendlyUnits_List.Where(unit => unit.type == Card.CardType.Base);
            var nonBaseCards = friendlyUnits_List.Where(unit => unit.type != Card.CardType.Base);

            friendlyUnits_nonOf = nonBaseCards.Concat(baseCards).ToArray();

            return (friendlyUnits_nonOf.Length > 0);
        }

        private bool ActiveBoard_FindFriendlyNonOffenseUnit(int ownerIndex, out CardInst friendlyUnit_nonOf)
        {
            if(ActiveBoard_AllFriendlyUnits(ownerIndex, out CardInst[] friendlyUnits))
            {
                foreach(CardInst unit in friendlyUnits){
                    if(unit.type != Card.CardType.Offense)
                        {
                            friendlyUnit_nonOf = unit;
                            return true;
                        }
                }
            }

            friendlyUnit_nonOf = CardInst.EMPTY;
            return false;
        }

        private bool ActiveBoard_FindOpenNeighbor(Axial origin, out Axial openPos)
        {
            if(Board.IsAxialOnGrid(origin))
            {
                for(int i = 0; i < Axial.CARDINAL_LENGTH; i++)
                {
                    Axial iDirection = Axial.Direction((Axial.Cardinal)i);

                    Axial neighbor = origin + iDirection;

                    if(Board.IsAxialOnGrid(neighbor))
                    {
                        if(!ActiveBoard_IsAxialOccupied(neighbor, out int dummyInt))
                        {
                            openPos = neighbor;
                            return true;
                        }
                    }
                }
            }

            openPos = Axial.Empty;
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

            DisplayCurrentActiveBoard();

            Thread.Sleep(500);

            turnPlayerIndex = turnCounter % PLAYER_COUNT;
            InitActiveCards();

            // 1. Draw a card

            for (int i = 0; i < CARDS_DRAWN_PER_TURN; i++)
            {
                TryDrawCard(turnPlayerIndex);
            }

            Thread.Sleep(500);

            GD.Print("---------------------------");
            GD.Print("--- Beginning Placement ---");

            // 2. Place card(s)

            if (Hands.Length > 0)
            {
                // Get a random number of cards to place
                int rand = random.Next(1, Hands[turnPlayerIndex].Length);

                // Iterate through each card to place
                for (int i = 0; i < rand; i++)
                {
                    int cardIndex = 0;
                    Card iCard = Hands[turnPlayerIndex][cardIndex];

                    // Follow offense placement rules
                    if (iCard.TYPE == Card.CardType.Offense)
                    {
                            bool canPlaceOffenseUnit = false;

                        if(ActiveBoard_AllNonOffenseFriendlyUnits(turnPlayerIndex, out CardInst[] resourceUnits))
                        {
                            foreach (CardInst resource in resourceUnits)
                            {
                                if (ActiveBoard_FindOpenNeighbor(resource.pos, out Axial openNeighbor))
                                {
                                    if(TryPlaceCard_FromHand(turnPlayerIndex, cardIndex, openNeighbor))
                                    {
                                        GD.Print($"Player {turnPlayerIndex} placing card {iCard.NAME} next to resource {resource}");
                                        HandleAttackAction(false, iCard.HP, Axial.Empty, resource);
                                        canPlaceOffenseUnit = true;
                                    }
                                    break;
                                }
                            }

                            if(!canPlaceOffenseUnit)
                                GD.Print($"Player {turnPlayerIndex} could not place offense unit because existing friendly units had no vacant neighbors OR there was a failure to place the card.");
                        }
                        else
                        {
                            GD.Print($"Could not place offense unit because there are no friendly non-offense units on the board.");
                        }
                    }
                    // Else, get a random open tile to place the unit
                    else if (TryGetOpenTile(out Axial openTile))
                    {
                        TryPlaceCard_FromHand(turnPlayerIndex, cardIndex, openTile);
                    }
                    else
                        GD.Print("Could not place card because all tiles are filled");
                    Thread.Sleep(500);
                }
            }

            Thread.Sleep(500);

            // 3. Move card(s)

            GD.Print("--------------------------");
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
                        GD.Print($"Player {turnPlayerIndex} moved {iCard.name} from {oldPos} to {newPos}.");
                    }
                    else{
                        GD.Print($"Player {turnPlayerIndex} could not move {iCard.name}");
                    }
                }
            }

            Thread.Sleep(500);

            GD.Print("-------------------------");
            GD.Print("--- Beginning Combat ---");

            foreach (CardInst occupant in ActiveBoard)
            {
                if (occupant.ownerIndex == turnPlayerIndex)
                {
                    CardInst iCard = occupant;

                    GD.Print($"Player {turnPlayerIndex} attempting to attack with {iCard.name} at {iCard.pos}.");

                    if (CardInst_TryRandomAttack(iCard))
                    {
                        GD.Print($"Player {turnPlayerIndex} made an attack with {iCard.name} from {iCard.pos}.");
                    }
                    else{
                        GD.Print($"Player {turnPlayerIndex} could not move {iCard.name}");
                    }
                }
            }

            // 4. Attack card(s)
            // 5. End turn
        }

        private void DisplayCurrentActiveBoard()
        {
            GD.Print("--- Displaying active board ---");
            for (int i = 0; i < ActiveBoard.Length; i++)
            {
                CardInst card = ActiveBoard[i];
                GD.Print($"[{i}]:{card}");
            }
            GD.Print("-------------------------------");
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
            if(card.CanAttack())
            {                
                if(ActiveBoard_FindEnemyNeighbor(card.ownerIndex, card.pos, out CardInst target)){
                    Axial attackDirection = target.pos - card.pos;
                    return HandleAttackAction(true, card, attackDirection, target);
                }
            }

            GD.Print($"Player {card.ownerIndex} can't attack with this unit.");
            return false;
        }

        private bool HandleAttackAction(bool doDisplace, CardInst attacker, Axial attackDirection, CardInst target)
        {
            GD.Print($"Player {attacker.ownerIndex} attacked {target.name} @ {target.pos} in direction {attackDirection}.");
            
            if(Axial.Distance(Axial.Zero, attackDirection) <= CardInst.ATK_RANGE)
            {
                attacker.Attack();

                return HandleAttackAction(doDisplace, attacker.atk, attackDirection, target);
            }
            else{
                GD.PrintErr($"Could not attack because attack distance ({Axial.Distance(Axial.Zero, attackDirection)}) is greater than 1.");
                return false;
            }
        }

        private bool HandleAttackAction(bool doDisplace, int damage, Axial attackDirection, CardInst target)
        {
            // If the target dies
            if (!target.Damage(damage))
            {
                if (target.type == Card.CardType.Base)
                {
                    GD.PrintErr($"Player {target.ownerIndex} has had their base destroyed! Need to fire an event to end the game.");
                    return true;
                }
                else
                {
                    ActiveBoard_RemoveCard(target, out CardInst dummyRemovedCard);
                }
            }
            // Else the target is alive, and if we should displace and the target is an offense unit, then
            else if (doDisplace && target.type == Card.CardType.Offense)
            {
                Axial attackDisplacement = target.pos + attackDirection;
                if (CardInst_TryMove(false, target, attackDisplacement, out CardInst occupant))
                {
                    GD.Print($"The attack successfully displaced the target.");
                }
                else if (occupant != CardInst.EMPTY)
                {
                    GD.Print($"The target could not be displaced because it collided with {occupant}. Damaging both units.");
                    
                    target.Damage(COLLISION_DAMAGE);
                    occupant.Damage(COLLISION_DAMAGE);
                }
                else
                {
                    GD.Print($"The target could not be displaced because it would have moved off the map. No consequences.");
                }
            }

            return true;
        }

        private bool TryGetOpenTile(out Axial Axial)
        {
            foreach (Axial ax in Board.Axials)
            {
                    if (!ActiveBoard_IsAxialOccupied(ax, out int dummyIndex))
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

            if(IsPlacementLocationValid(location) && OffensePlacementRule(player_index, location, card_index)){

                Card card = RemoveCardFromHand(player_index, card_index);
                CardInst cardInst = new CardInst(player_index, location, card);

                ActiveBoard_AddCard(cardInst);

                return true;
            }
            else
                return false;
        }

        private bool OffensePlacementRule(int player_index, Axial placement, Card card)
        {
            if(card.TYPE == Card.CardType.Offense){
                if(ActiveBoard_FindFriendlyNonOffenseNeighbor(player_index, placement, out CardInst friendlyUnit))
                {
                    return true;
                }
                else
                {
                    // This is an offense card, but there are no nearby non-offense friendly units, so it fails the rule
                    return false;
                }
            }
            else{
                // This is not an offense card, so it passes the rule
                return true;
            }
        }

        private bool OffensePlacementRule(int player_index, Axial placement, int card_index)
        {
            ref Card[] refHand = ref Hands[player_index];
            Card card = refHand[card_index];

            return OffensePlacementRule(player_index, placement, card);
        }

        private bool TryPlaceCard_FromVoid(int player_index, Axial location, Card card){

            if(IsPlacementLocationValid(location) && OffensePlacementRule(player_index, location, card)){
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

            GD.Print($"Player {newCard.ownerIndex} placing card {newCard.name} at location {newCard.pos}");
        }

        private bool ActiveBoard_RemoveCard(CardInst card, out CardInst removedCard){

            int card_index = -1;

            for (int i = 0; i < ActiveBoard.Length; i++)
            {
                if(card == ActiveBoard[i]){
                    card_index = i;
                    break;
                }
            }

            if(card_index >= 0){
                return ActiveBoard_RemoveCard(card_index, out removedCard);
            }
            else{
                GD.PrintErr($"Cannot remove card {card} because it is not on the board.");
                removedCard = CardInst.EMPTY;
                return false;
            }
        }

        private object activeBoardLock = new object();

        private bool ActiveBoard_RemoveCard(int cardToRemove_index, out CardInst removedCard)
        {
            CardInst cardToRemove = ActiveBoard[cardToRemove_index];
            CardInst[] newActiveBoard = new CardInst[ActiveBoard.Length - 1];

            lock (activeBoardLock){

                for (int i = 0, j = 0; i < ActiveBoard.Length; i++)
                {
                    if (i != cardToRemove_index)
                    {
                        newActiveBoard[j++] = ActiveBoard[i];
                    }
                }

                ActiveBoard = newActiveBoard;
            }

            foreach(CardInst card in ActiveBoard)
            {
                if(card == cardToRemove){
                    GD.PrintErr($"Failed to remove {cardToRemove} from active board");
                    removedCard = CardInst.EMPTY;
                    return false;
                }
            }

            removedCard = cardToRemove;
            GD.Print($"Successfully removed {removedCard.name} from the board");
            return true;
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
            if (ActiveBoard_IsAxialOccupied(location, out int boardIndex))
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