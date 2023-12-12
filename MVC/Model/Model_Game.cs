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
using static Model.ActionPoster;

namespace Model
{
    /// <summary>
    /// The Model handles the data of the game without any concern/coupling with the representation of that data.
    /// </summary>
    /// <remarks>This script is multithreaded because it has no connection to Godot's API and because there's an advantage to multithreading game logic
    /// : processes can be awaited using Thread.Sleep instead of await methods, which negates many of the restrictions that come with the async/await pattern</remarks>
    public class Model_Game
    {
        //---------------------
        //----- VARIABLES -----
        //---------------------

        #region VARIABLES

        // STATIC
        private static readonly int _DECK_COUNT = 20;
        private static readonly int _HAND_START_COUNT = 2;
        private static readonly int _CARDS_DRAWN_PER_TURN = 1;
        private static readonly int _CARDS_DRAWN_LIMIT = 6;
        private static readonly int _PLAYER_COUNT = 2;
        private static readonly int _BOARD_RADIUS = 2;

        private static readonly int _COLLISION_DAMAGE = 1;

        private static readonly int _waitTime = 5;

        // CARD SET
        private static readonly Card[] _CardSet = new Card[] {
            new Card(false, Card.BASE_TEST_NAME, Card.CardType.Base, 10),
            new Card(false, Card.RESOURCE_TEST_NAME, Card.CardType.Resource, 3),
            new Card(false, Card.OFFENSE_TEST_NAME, 2, 3, 1)
        };

        private static readonly Card[] _CardSet_NoBases = _CardSet
            .Where(card => card.TYPE != Card.CardType.Base)
            .ToArray();


        // INSTANCE
        Random _random = new Random();

        #region  SESSION DATA       // Data scoped to a session instance

        private int _roundCounter = 0;

        private Card[][] _Decks = new Card[_PLAYER_COUNT][];
        private Card[] _userDeck
        {
            get
            {
                return _Decks[0];
            }
            set
            {
                _Decks[0] = value;
            }
        }
        private Card[] _enemyDeck
        {
            get
            {
                return _Decks[1];
            }
            set
            {
                _Decks[1] = value;
            }
        }

        #endregion

        #region ROUND DATA          // Data scoped to a round instance
        
        private Axial[] _base_locations = new Axial[] {
            Axial.Direction(Axial.Cardinal.SW),
            Axial.Direction(Axial.Cardinal.NE)
        };

        private int _turnCounter = 0;
        public int TurnCounter => _turnCounter;

        private int[] _CardsDrawn = new int[_PLAYER_COUNT];       // How many cards each player has drawn

        // Hands data
        private Card[][] _Hands = new Card[_PLAYER_COUNT][];
        private Card[] _userHand
        {
            get
            {
                return _Hands[0];
            }
            set
            {
                _Hands[0] = value;
            }
        }
        public Card[] EnemyHand
        {
            get
            {
                return _Hands[1];
            }
            set
            {
                _Hands[1] = value;
            }
        }

        // Board Data
        private AxialGrid _Board;
        public AxialGrid Board => _Board;

        public Unit[] ActiveBoard => _activeBoard;
        private Unit[] _activeBoard = new Unit[0];

        #endregion

        #region  TURN DATA          // Data scoped to a turn instance

        /// <summary>
        /// The index of the player whose turn it is
        /// </summary>
        public int TurnPlayerIndex => _turnPlayerIndex;
        private int _turnPlayerIndex = 0;

        #endregion

        #endregion

        //--------------------------
        //----- INITIALIZATION -----
        //--------------------------

        public Action<Card[], Card[]> OnGameStart;
        public Action<int> OnRoundStart;
        public Action<int, int> OnTurnStart;
        public Action<int, int> OnTurnEnd;

        public Action<int> OnDeckBuildStart;
        public Action<int, int, Card> OnDeckBuildAddedCard;
        public Action<int> OnDeckBuildFinished;

        /// <summary>
        /// (<see cref="Unit"/> newUnit)
        /// </summary>
        public Action<Unit> OnUnitAddedToBoard;
        /// <summary>
        /// (<see cref="Axial"/> oldPos, <see cref="Unit"/> movedUnit)
        /// </summary>
        public Action<Axial, Unit> OnUnitMove;
        /// <summary>
        /// (<see cref="Unit"/> attacker, <see cref="Unit"/> target)
        /// </summary>
        public Action<Unit, Unit> OnUnitAttack;
        public Action<Unit> OnBaseDestroyed;
        public Action<Unit> OnUnitDamaged;
        public Action<Unit> OnUnitBuffed;
        public Action<Unit> OnUnitDeath;
        public Action<Unit, Unit> OnCollision;

        public Action<int, Card, Card[], int> OnCardDrawn;
        public Action<int, int, int> OnCardDrawn_fail;
        public Action<int, Card, Card[]> OnCardRemoved;

        // Await Input Actions
        public Action OnAwaitStartGame;
        public Action<int, int> OnAwaitDrawCard;

        public Action<int, int> OnAwaitTurnActions;


        // Await Triggers
        public bool triggerStartGame = false,
        TriggerDrawCard = false,
        TriggerEndTurn = false;

        #region  Post Action

        #endregion

        #region INITIALIZATION

        bool _isGameStarted = false;

        // CONSTRUCTOR
        public Model_Game()
        {
            GD.Print("Game Model Constructed");

            Task.Run(() =>
            {
                // Repost action until it has a subscriber
                while(!PostAction(OnAwaitStartGame))
                {
                    Thread.Sleep(_waitTime * 100);
                }

                AwaitAction(ref triggerStartGame, StartGame);
            });
        }

        private bool StartGame()
        {
            if (!_isGameStarted)
            {
                _isGameStarted = true;

                PostAction(OnGameStart, _CardSet, _CardSet_NoBases);

                _roundCounter = 0;

                InitDecks();

                StartRound(ref _roundCounter);

                return true;
            }
            else
            {
                return false;
            }
        }

        private void InitDecks()
        {
            for (int i = 0; i < _PLAYER_COUNT; i++)
            {
                PostAction(OnDeckBuildStart, i);

                ref Card[] refDeck = ref _Decks[i];
                refDeck = new Card[_DECK_COUNT];
                for (int j = 0; j < refDeck.Length; j++)
                {
                    int rand = _random.Next(0, 100);
                    Card cardFromSet = rand < 70 ? _CardSet_NoBases[1] : _CardSet_NoBases[0];
                    refDeck[j] = new Card(true, cardFromSet);

                    PostAction(OnDeckBuildAddedCard, i, j, refDeck[j]);
                }

                PostAction(OnDeckBuildFinished, i);
            }
        }

        private void StartRound(ref int roundCounter)
        {
            PostAction(OnRoundStart, roundCounter);

            _turnCounter = 0;
            _Board = new AxialGrid(_BOARD_RADIUS);
            _activeBoard = new Unit[0];
            InitHands();
            InitBases();

            while(!IsRoundOver(_turnCounter)){
                StartTurn(ref _turnCounter);
                _turnCounter++;
            }
        }
        
        private void InitHands()
        {
            for (int i = 0; i < _PLAYER_COUNT; i++)
            {
                ref Card[] refHand = ref _Hands[i];
                ref Card[] refDeck = ref _Decks[i];

                refHand = new Card[0];

                for (int j = 0; j < _HAND_START_COUNT; j++)
                {
                    TryDrawCard(i);
                    Thread.Sleep(200);
                }

                DisplayHand(i);
            }
        }

        private void InitBases(){
            Thread.Sleep(100);

            for(int i = 0; i < _PLAYER_COUNT; i++){
                Card BaseFromSet = _CardSet[0];
                Card BaseCard = new Card(true, BaseFromSet);
                Axial BaseLocation = CardSet_GetBaseLocation(i);

                if(TryPlaceCard_FromVoid(i, BaseLocation, BaseCard))
                    GD.Print($"Successfully placed base card from void @ {BaseLocation}");
                    else
                    GD.PrintErr($"Failed to place base card from void @ {BaseLocation}");
            }
        }

        private void StartTurn(ref int turnCounter)
        {
            _turnPlayerIndex = turnCounter % _PLAYER_COUNT;
            PostAction(OnTurnStart, _turnPlayerIndex, turnCounter);

            // 1. Draw a card
            for (int iDraw = 0; iDraw < _CARDS_DRAWN_PER_TURN; iDraw++)
            {
                // The intention I have is to post an action to begin awaiting some external script to trigger drawing a card
                // My logic is that this way other scripts do not need to constantly be checking for this condition when its not even possible to trigger it
                // It also removes the need to manage state, which I prefer not to deal with because I think it is overly complex and unintuitive
                PostAction(OnAwaitDrawCard, _turnPlayerIndex, iDraw);
                if (AwaitAction(ref TriggerDrawCard, TryDrawCard, _turnPlayerIndex))
                    GD.Print("Successfully drew card after awaiting trigger.");
                else
                    GD.PrintErr("Failed to draw card after awaiting trigger");
            }

            PostAction(OnAwaitTurnActions, _turnPlayerIndex, turnCounter);
            if (AwaitAction(ref TriggerEndTurn, TryEndTurn))
                GD.Print("Successfully ended the turn");
            else
                GD.PrintErr("Failed to end the turn");

            return;
        }

        private bool TryEndTurn()
        {
            PostAction(OnTurnEnd, _turnPlayerIndex, _turnCounter);
            // No need to increment turn counter or start next turn, turns run in a while loop until the round is over.
            return true;
        }

        private Axial CardSet_GetBaseLocation(int player_index){

            if(_PLAYER_COUNT > _base_locations.Length){
                GD.PrintErr($"Not enough base locations ({_base_locations.Length}) for the amount of players({_PLAYER_COUNT})");
                return Axial.Empty;
            }

            return _base_locations[player_index] * _BOARD_RADIUS;
        }

        private bool ActiveBoard_TryGetBaseUnit(int player_index, out Unit playerBase)
        {
            Axial baseLocation = CardSet_GetBaseLocation(player_index);

            if (ActiveBoard_IsAxialOccupied(baseLocation, out int baseIndex))
            {
                playerBase = _activeBoard[baseIndex];
                return true;
            }
            else
            {
                GD.PrintErr($"Player {player_index} does not have a unit at its base location {baseLocation}");

                playerBase = Unit.EMPTY;
                return false;
            }
        }

        /// <summary>
        /// 'out' index within <see cref="_activeBoard"/> of the unit at the axial position, if one exists
        /// </summary>
        /// <returns>True if a unit exists at the parameter axial, false if not</returns>
        public bool ActiveBoard_IsAxialOccupied(Axial axial, out int boardIndex)
        {
            for (int i = 0; i < _activeBoard.Length; i++)
            {
                Unit unit = _activeBoard[i];
                if (unit.pos == axial){
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
        public bool ActiveBoard_FindNeighbors(Axial axial, out int[] neighborBoardIndexes)
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
        /// <remarks>Used in <see cref="Unit_TryRandomAttack"/></remarks>
        public bool ActiveBoard_FindEnemyNeighbor(int ownerIndex, Axial originAxial, out Unit enemyNeighborUnit)
        {
            int[] neighborBoardIndexes;
            if(ActiveBoard_FindNeighbors(originAxial, out neighborBoardIndexes)){
                foreach(int index in neighborBoardIndexes){
                    Unit neighborUnit = _activeBoard[index];
                    if(neighborUnit.ownerIndex != ownerIndex){
                        enemyNeighborUnit = neighborUnit;
                        return true;
                    }
                }
            }

            enemyNeighborUnit = Unit.EMPTY;
            return false;
        }

        /// <summary>
        /// 'out' the first friendly, non-offense neighbor found
        /// </summary>
        /// <returns>True if friendly, non-offense neighbor found, false if not</returns>
        public bool ActiveBoard_FindFriendlyNonOffenseNeighbor(int ownerIndex, Axial axial, out Unit friendlyUnit)
        {
            int[] neighborBoardIndexes;
            if(ActiveBoard_FindNeighbors(axial, out neighborBoardIndexes)){
                foreach(int index in neighborBoardIndexes){
                    Unit neighbor = _activeBoard[index];
                    if(neighbor.ownerIndex == ownerIndex && neighbor.type != Card.CardType.Offense){
                        friendlyUnit = neighbor;
                        return true;
                    }
                }
            }

            friendlyUnit = Unit.EMPTY;
            return false;
        }

        public bool ActiveBoard_AllFriendlyUnits(int ownerIndex, out Unit[] friendlyUnits)
        {
            List<Unit> friendlyUnits_List = new List<Unit>(0);

            foreach(Unit unit in _activeBoard)
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
        /// <remarks>The 'out' list sorts 'base' units to the end of the array</remarks>
        public bool ActiveBoard_AllNonOffenseFriendlyUnits(int ownerIndex, out Unit[] friendlyUnits_nonOf)
        {
            List<Unit> friendlyUnits_List = new List<Unit>(0);

            if (ActiveBoard_AllFriendlyUnits(ownerIndex, out Unit[] friendlyUnits))
            {
                foreach (Unit unit in friendlyUnits)
                {
                    if (unit.type != Card.CardType.Offense)
                    {
                        friendlyUnits_List.Add(unit);
                    }
                }
            }

            // Separate 'base' units so that they can be appended to the back (for priority as last pick)
            var baseCards = friendlyUnits_List.Where(unit => unit.type == Card.CardType.Base);
            var nonBaseCards = friendlyUnits_List.Where(unit => unit.type != Card.CardType.Base);

            friendlyUnits_nonOf = nonBaseCards.Concat(baseCards).ToArray();

            return (friendlyUnits_nonOf.Length > 0);
        }

        public bool ActiveBoard_FindFriendlyNonOffenseUnit(int ownerIndex, out Unit friendlyUnit_nonOf)
        {
            if(ActiveBoard_AllFriendlyUnits(ownerIndex, out Unit[] friendlyUnits))
            {
                foreach(Unit unit in friendlyUnits){
                    if(unit.type != Card.CardType.Offense)
                        {
                            friendlyUnit_nonOf = unit;
                            return true;
                        }
                }
            }

            friendlyUnit_nonOf = Unit.EMPTY;
            return false;
        }

        public bool ActiveBoard_FindOpenNeighbor(Axial origin, out Axial openPos)
        {
            if(_Board.IsAxialOnGrid(origin))
            {
                for(int i = 0; i < Axial.CARDINAL_LENGTH; i++)
                {
                    Axial iDirection = Axial.Direction((Axial.Cardinal)i);

                    Axial neighbor = origin + iDirection;

                    if(_Board.IsAxialOnGrid(neighbor))
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

        public bool IsRoundOver(int turnCounter){

            for(int i = 0; i < _PLAYER_COUNT; i++){
                if(ActiveBoard_TryGetBaseUnit(i, out Unit playerBase)){
                    if(playerBase.hp <= 0){
                        return true;
                    }
                }
            }
            
            return turnCounter >= 100;
        }

        public bool Unit_TryMove(bool isWillful, int playerIndex, Axial unitPos, Axial destination)
        {
            // Continue if the move is NOT willful OR it is the player's turn (a willful move should only be made on the player's turn)
            if (!isWillful || playerIndex == _turnPlayerIndex)
            {
                if (ActiveBoard_IsAxialOccupied(unitPos, out int boardIndex))
                {
                    Unit unit = _activeBoard[boardIndex];
                    if (!isWillful || unit.ownerIndex == _turnPlayerIndex)
                        return Unit_TryMove(isWillful, unit, destination);
                    else
                    {
                        GD.PrintErr($"Cannot move unit @ {unitPos} because it is not this player's({playerIndex}) turn (it is player [{_turnPlayerIndex}]'s turn)");
                        return false;
                    }
                }
                else
                {
                    GD.PrintErr($"Cannot move unit @ {unitPos} because no unit exists at {unitPos}");
                    return false;
                }
            }
            else
            {
                GD.PrintErr($"Cannot move unit @ {unitPos} because it is not this player's({playerIndex}) turn (it is player [{_turnPlayerIndex}]'s turn)");
                return false;
            }
        }

        public bool Unit_TryMove(bool isWillful, Unit unit, Axial newPos)
        {
            return Unit_TryMove(isWillful, unit, newPos, out Unit dummyOccupant);
        }

        public bool Unit_TryMove(bool isWillful, Unit unit, Axial newPos, out Unit occupant)
        {
            Axial oldPos = unit.pos;

            if (unit.TryMove(isWillful, unit, newPos, out occupant))
            {
                return PostAction(OnUnitMove, oldPos, unit);
            }
            else
                return false;
        }

        public bool Unit_TryAttack(bool doDisplace, Unit attacker, Axial attackDirection, Unit target)
        {
            if(attacker == target)
            {
                GD.PrintErr($"Unit {attacker.name} is trying to attack itself");
                return false;
            }

            GD.Print($"Player {attacker.ownerIndex} attacked {target.name} @ {target.pos} in direction {attackDirection}.");
            
            if(
                Axial.Distance(Axial.Zero, attackDirection) <= Unit.ATK_RANGE
                && attacker.CanAttack())
            {
                attacker.Attack();
                PostAction(OnUnitAttack, attacker, target);

                return HandleAttackAction(doDisplace, attacker.atk, attackDirection, target);
            }
            else{
                GD.PrintErr($"Unit {attacker.name}@{attacker.pos} Could not attack because attack distance ({Axial.Distance(Axial.Zero, attackDirection)}) is greater than 1 OR card TurnActions said that this unit could not attack.");
                return false;
            }
        }

        private bool HandleAttackAction(bool doDisplace, int damage, Axial attackDirection, Unit target)
        {
            PostAction(OnUnitDamaged, target);
            // If the target dies
            if (!target.Damage(damage))
            {
                if (target.type == Card.CardType.Base)
                {
                    GD.PrintErr($"Player {target.ownerIndex} has had their base destroyed! Need to fire an event to end the game.");
                    PostAction(OnBaseDestroyed, target);
                    return true;
                }
                else
                {
                    ActiveBoard_RemoveUnit(target, out Unit dummyRemovedUnit);
                }
            }
            // Else the target is alive, and if we should displace and the target is an offense unit, then
            else if (doDisplace && target.type == Card.CardType.Offense)
            {
                Axial attackDisplacement = target.pos + attackDirection;
                if (Unit_TryMove(false, target, attackDisplacement, out Unit occupant))
                {
                    GD.Print($"The attack successfully displaced the target.");
                }
                else if (occupant != Unit.EMPTY)
                {
                    GD.Print($"The target could not be displaced because it collided with {occupant}. Damaging both units.");
                    
                    target.Damage(_COLLISION_DAMAGE);
                    occupant.Damage(_COLLISION_DAMAGE);
                    PostAction(OnCollision, target, occupant);
                }
                else
                {
                    GD.Print($"The target could not be displaced because it would have moved off the map. No consequences.");
                }
            }

            return true;
        }

        public bool ActiveBoard_TryGetOpenTile(out Axial Axial)
        {
            foreach (Axial ax in _Board.Axials)
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

        private void DisplayHand(int player_index){

                GD.Print(new string('-', 28));
                GD.Print($"----- Displaying hand[{player_index}] -----");

                ref Card[] refHand = ref _Hands[player_index];

                for (int i = 0; i < refHand.Length; i++)
                {
                    GD.Print($"[{i}] : {refHand[i].NAME}");
                }

                GD.Print(new string('-', 28));
        }

        private bool TryDrawCard(int player_index){
            Card[] iDeck = _Decks[player_index];
            ref Card[] refHand = ref _Hands[player_index];
            int deckCount = iDeck.Length;
            ref int refDrawnCount = ref _CardsDrawn[player_index]; 

            if(refDrawnCount < deckCount && refHand.Length < _CARDS_DRAWN_LIMIT){
                Card drawnCard = iDeck[refDrawnCount];

                int heldCount = AddCardToHand(player_index, drawnCard);

                refDrawnCount++;

                PostAction(OnCardDrawn, player_index, drawnCard, _Hands[player_index], refDrawnCount);

                return true;
            }
            else{
                PostAction(OnCardDrawn_fail, player_index, refDrawnCount, deckCount);

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
            ref Card[] refHand = ref _Hands[player_index];

            if (refHand != null && refHand.Length > 0)
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
            ref Card[] refHand = ref _Hands[player_index];

            //If index is within bounds,
            if (card_index < refHand.Length && card_index >= 0)
            {
                //Get reference
                Card removedCard = refHand[card_index];

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

                GD.Print($"Removed {removedCard.NAME} from player {player_index}'s hand");

                PostAction(OnCardRemoved, player_index, removedCard, newHand);

                //Return removed card
                return removedCard;
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
        public bool TryPlaceCard_FromHand(int player_index, int card_index, Axial location)
        {
            ref Card[] refHand = ref _Hands[player_index];

            if (IsLocationValidAndOpen(location))
            {
                Card.CardType cardType = refHand[card_index].TYPE;

                if (cardType == Card.CardType.Offense)
                {
                    if (OffensePlacementRule(player_index, location, card_index, out Unit friendlyResourceUnit))
                    {
                        PlaceUnit_FromHand(player_index, card_index, location, friendlyResourceUnit);
                    }
                }
                else
                {
                    PlaceUnit_FromHand(player_index, card_index, location);
                }

                return true;
            }
            else
                return false;
        }

        public bool TryPlaceCard_FromHand(int player_index, uint cardID, Axial location)
        {
            if(GetCardByID_FromHand(player_index, cardID, out Card cardFromHand, out int cardFromHand_Index))
            {
                return TryPlaceCard_FromHand(player_index, cardFromHand_Index, location);
            }
            else
                return false;
        }

        private void PlaceUnit_FromHand(int player_index, int card_index, Axial location)
        {
            Card card = RemoveCardFromHand(player_index, card_index);
            Unit Unit = new Unit(player_index, location, card);

            ActiveBoard_AddUnit(Unit);
        }

        private void PlaceUnit_FromHand(int player_index, int card_index, Axial location, Unit friendlyResourceUnit)
        {
            Card card = RemoveCardFromHand(player_index, card_index);
            Unit Unit = new Unit(player_index, location, card);

            ActiveBoard_AddUnit(Unit);

            HandleAttackAction(false, card.HP, Axial.Empty, friendlyResourceUnit);
        }

        public void PlaceUnit_FromVoid(int player_index, Card card, Axial location, Unit friendlyResourceUnit)
        {
            Unit Unit = new Unit(player_index, location, card);

            ActiveBoard_AddUnit(Unit);

            if(friendlyResourceUnit != null)
                HandleAttackAction(false, card.HP, Axial.Empty, friendlyResourceUnit);
        }

        public bool GetCardByID_FromHand(int player_index, uint cardID, out Card cardFromHand, out int cardFromHand_Index)
        {
            cardFromHand = Card.EMPTY;

            if(cardID == 0)
            {
                GD.PrintErr("Trying to get card by ID when paramter ID is 0. This method should not be run on cards from the card set (the only cards which should have ID 0)");
                cardFromHand_Index = -1;
                return false;
            }

            for (cardFromHand_Index = 0; cardFromHand_Index < _Hands[player_index].Length; cardFromHand_Index++)
            {
                Card card = _Hands[player_index][cardFromHand_Index];
                if (card.id == cardID)
                {
                    cardFromHand = card;
                    return true;
                }
            }

            cardFromHand_Index = -1;
            return false;
        }

        private bool CheckOffensePlacementRule(int player_index, Axial placement, Card card, out Unit friendlyUnit)
        {
            if(card.TYPE == Card.CardType.Offense){
                if(ActiveBoard_FindFriendlyNonOffenseNeighbor(player_index, placement, out friendlyUnit))
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
                friendlyUnit = null;
                return true;
            }
        }

        private bool OffensePlacementRule(int player_index, Axial placement, int card_index, out Unit friendlyUnit)
        {
            ref Card[] refHand = ref _Hands[player_index];
            Card card = refHand[card_index];

            return CheckOffensePlacementRule(player_index, placement, card, out friendlyUnit);
        }

        private bool TryPlaceCard_FromVoid(int player_index, Axial location, Card card){

            if(IsLocationValidAndOpen(location) && CheckOffensePlacementRule(player_index, location, card, out Unit friendlyUnit)){

                PlaceUnit_FromVoid(player_index, card, location, friendlyUnit);

                return true;
            }
            else
                return false;
        }

        private void ActiveBoard_AddUnit(Unit newUnit)
        {
            if (_activeBoard != null)
            {
                Unit[] newActiveBoard = new Unit[_activeBoard.Length + 1];
                _activeBoard.CopyTo(newActiveBoard, 0);
                newActiveBoard[newActiveBoard.Length - 1] = newUnit;

                _activeBoard = newActiveBoard;
                PostAction(OnUnitAddedToBoard, newUnit);
            }
            else
            {
                _activeBoard = new Unit[1] { newUnit };
            }
        }

        private bool ActiveBoard_RemoveUnit(Unit unit, out Unit removedUnit){

            int unit_BoardIndex = -1;

            for (int i = 0; i < _activeBoard.Length; i++)
            {
                if(unit == _activeBoard[i]){
                    unit_BoardIndex = i;
                    break;
                }
            }

            if(unit_BoardIndex >= 0){
                return ActiveBoard_RemoveUnit(unit_BoardIndex, out removedUnit);
            }
            else{
                GD.PrintErr($"Cannot remove unit {unit} because it is not on the board.");
                removedUnit = Unit.EMPTY;
                return false;
            }
        }

        private object activeBoardLock = new object();

        private bool ActiveBoard_RemoveUnit(int unitToRemove_index, out Unit removedUnit)
        {
            Unit unitToRemove = _activeBoard[unitToRemove_index];
            Unit[] newActiveBoard = new Unit[_activeBoard.Length - 1];

            lock (activeBoardLock){

                for (int i = 0, j = 0; i < _activeBoard.Length; i++)
                {
                    if (i != unitToRemove_index)
                    {
                        newActiveBoard[j++] = _activeBoard[i];
                    }
                }

                _activeBoard = newActiveBoard;
            }

            foreach(Unit unit in _activeBoard)
            {
                if(unit == unitToRemove){
                    GD.PrintErr($"Failed to remove {unitToRemove} from active board");
                    removedUnit = Unit.EMPTY;
                    return false;
                }
            }

            removedUnit = unitToRemove;
            GD.Print($"Successfully removed {removedUnit.name} from the board");
            PostAction(OnUnitDeath, removedUnit);
            return true;
        }

        public bool IsLocationValidAndOpen(Axial location)
        {
            return IsLocationValidAndOpen(location, out Unit dummyUnit);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="location">Axial to place at</param>
        /// <param name="occupant">If the tile is occupied, this is the unit occupying it</param>
        /// <returns></returns>
        public bool IsLocationValidAndOpen(Axial location, out Unit occupant)
        {
            occupant = Unit.EMPTY;

            // Check if the location exists on the board
            if (!_Board.IsAxialOnGrid(location))
            {
                GD.Print($"Cannot place card at {location} because it does not exist on the board.");
                return false;
            }

            // Check active board to see if a card has already been placed at this location
            if (ActiveBoard_IsAxialOccupied(location, out int boardIndex))
            {
                GD.Print($"Cannot place unit at {location} because a unit is already placed there.");
                occupant = _activeBoard[boardIndex];
                return false;
            }

            return true;
        }

        #endregion
    }
}