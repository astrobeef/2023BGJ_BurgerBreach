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
        private static readonly int _HAND_START_COUNT = 4;
        private static readonly int _CARDS_DRAWN_PER_TURN = 1;
        private static readonly int _PLAYER_COUNT = 2;
        private static readonly int _BOARD_RADIUS = 2;

        private static readonly int _COLLISION_DAMAGE = 1;

        private static readonly int _waitTime = 5;

        // CARD SET
        private static readonly Card[] _CardSet = new Card[] {
            new Card("Base Test", Card.CardType.Base, 10),
            new Card("Resource Test", Card.CardType.Resource, 3),
            new Card("Offense Test", 2, 3, 1)
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
        private Card[] _enemyHand
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
        private Unit[] _ActiveBoard = new Unit[0];

        #endregion

        #region  TURN DATA          // Data scoped to a turn instance

        /// <summary>
        /// The index of the player whose turn it is
        /// </summary>
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

        public Action<Unit> OnUnitAddedToBoard;
        public Action<Axial, Unit> OnUnitMove;
        public Action<Unit, Unit> OnUnitAttack;
        public Action<Unit> OnBaseDestroyed;
        public Action<Unit> OnDamaged;
        public Action<Unit> OnDeath;
        public Action<Unit, Unit> OnCollision;

        public Action<int, string, Card[], int> OnCardDrawn;
        public Action<int, int, int> OnCardDrawn_fail;
        public Action<int, Card[]> OnCardRemoved;

        // Await Input Actions
        public Action OnAwaitStartGame;
        public Action<int, int> OnAwaitDrawCard;

        public Action<int, int> OnAwaitTurnActions;

        SynchronizationContext context = SynchronizationContext.Current;

        // Await Triggers
        public bool triggerStartGame = false,
        TriggerDrawCard = false,
        TriggerEndTurn = false;

        #region  Post Action

        public bool PostAction(Action action)
        {
            if (action != null)
            {
                context.Post(_ => action(), null);
                return true;
            }
            
            return false;
        }
        public bool PostAction<T1>(Action<T1> action, T1 param1)
        {
            if (action != null)
            {
                context.Post(_ => action(param1), null);
                return true;
            }
            
            return false;
        }
        public bool PostAction<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2)
        {
            if (action != null)
            {
                context.Post(_ => action(param1, param2), null);
                return true;
            }

            return false;
        }
        public bool PostAction<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3)
        {
            if (action != null)
            {
                context.Post(_ => action(param1, param2, param3), null);
                return true;
            }

            return false;
        }
        public bool PostAction<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            if (action != null)
            {
                context.Post(_ => action(param1, param2, param3, param4), null);
                return true;
            }
            
            return false;
        }

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

                context.Post(_ => OnGameStart(_CardSet, _CardSet_NoBases), null);

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
                    int rand = _random.Next(0, _CardSet_NoBases.Length);
                    refDeck[j] = _CardSet_NoBases[rand];

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
            _ActiveBoard = new Unit[0];
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

                refHand = null;

                for (int j = 0; j < _HAND_START_COUNT; j++)
                {
                    TryDrawCard(i);
                }

                DisplayHand(i);
            }
        }

        private void InitBases(){
            for(int i = 0; i < _PLAYER_COUNT; i++){
                Card BaseCard = _CardSet[0];
                Axial BaseLocation = CardSet_GetBaseLocation(i);

                TryPlaceCard_FromVoid(i, BaseLocation, BaseCard);
            }
        }

        private bool AwaitAction(ref bool trigger, Func<bool> action)
        {
            while (!trigger)
            {
                Thread.Sleep(1);
            }
            trigger = false;
            return action();
        }
        private bool AwaitAction<T1>(ref bool trigger, Func<T1, bool> action, T1 param1)
        {
            while (!trigger)
            {
                Thread.Sleep(1);
            }
            trigger = false;
            return action(param1);
        }
        private bool AwaitAction<T1, T2>(ref bool trigger, Func<T1, T2, bool> action, T1 param1, T2 param2)
        {
            while (!trigger)
            {
                Thread.Sleep(1);
            }
            trigger = false;
            return action(param1, param2);
        }
        private bool AwaitAction<T1, T2, T3>(ref bool trigger, Func<T1, T2, T3, bool> action, T1 param1, T2 param2, T3 param3)
        {
            while (!trigger)
            {
                Thread.Sleep(1);
            }
            trigger = false;
            return action(param1, param2, param3);
        }

        private void StartTurn(ref int turnCounter)
        {
            _turnPlayerIndex = turnCounter % _PLAYER_COUNT;
            PostAction(OnTurnStart, turnCounter, _turnPlayerIndex);

            ResetAllActiveUnitsTurnActions();

            // 1. Draw a card
            for (int iDraw = 0; iDraw < _CARDS_DRAWN_PER_TURN; iDraw++)
            {
                // The intention I have is to post an action to begin awaiting some external script to trigger drawing a card
                // My logic is that this way other scripts do not need to constantly be checking for this condition when its not even possible to trigger it
                // It also removes the need to manage state, which I prefer not to deal with because I think it is overly complex and unintuitive
                PostAction(OnAwaitDrawCard, _turnPlayerIndex, iDraw);
                if (AwaitAction(ref TriggerDrawCard, TryDrawCard, _turnPlayerIndex))
                    GD.PrintErr("Successfully drew card after awaiting trigger.");
                else
                    GD.PrintErr("Failed to draw card after awaiting trigger");
            }

            // Input should allow for placement OR movement OR attack all at the same time

            PostAction(OnAwaitTurnActions, _turnPlayerIndex, turnCounter);
            if (AwaitAction(ref TriggerEndTurn, TryEndTurn))
                GD.PrintErr("Successfully ended the turn");
            else
                GD.PrintErr("Failed to end the turn");

            return;

            // 2. Place card(s)

            // 3. Move card(s)

            foreach (Unit occupant in _ActiveBoard)
            {
                if (occupant.ownerIndex == _turnPlayerIndex)
                {
                    Thread.Sleep(_waitTime);

                    Unit iUnit = occupant;

                    Axial oldPos = iUnit.pos;

                    GD.Print($"Player {_turnPlayerIndex} attempting to move {iUnit.name} at {oldPos}.");

                    if (Unit_TryRandomMove(iUnit, out Axial newPos))
                    {
                        GD.Print($"Player {_turnPlayerIndex} moved {iUnit.name} from {oldPos} to {newPos}.");
                    }
                    else{
                        GD.Print($"Player {_turnPlayerIndex} could not move {iUnit.name}");
                    }
                }
            }

            foreach (Unit occupant in _ActiveBoard)
            {
                if (occupant.ownerIndex == _turnPlayerIndex)
                {
                    Thread.Sleep(_waitTime);

                    Unit iUnit = occupant;

                    GD.Print($"Player {_turnPlayerIndex} attempting to attack with {iUnit.name} at {iUnit.pos}.");

                    if (Unit_TryRandomAttack(iUnit))
                    {
                        GD.Print($"Player {_turnPlayerIndex} made an attack with {iUnit.name} from {iUnit.pos}.");
                    }
                    else{
                        GD.Print($"Player {_turnPlayerIndex} could not move {iUnit.name}");
                    }
                }
            }

            // 4. Attack card(s)
            // 5. End turn
        }

        public bool TryPlaceRandomCardRandomly()
        {
            if (_Hands.Length > 0)
            {
                int cardIndex = _random.Next(0,_Hands.Length);

                return TryPlaceCardRandomly(_turnPlayerIndex, cardIndex);
            }

            return false;
        }

        public bool TryPlaceCardRandomly(int playerIndex, int cardIndex)
        {
            Card card = _Hands[playerIndex][cardIndex];

            // Follow offense placement rules
            if (card.TYPE == Card.CardType.Offense)
            {
                bool canPlaceOffenseUnit = false;

                if (ActiveBoard_AllNonOffenseFriendlyUnits(_turnPlayerIndex, out Unit[] resourceUnits))
                {
                    foreach (Unit resource in resourceUnits)
                    {
                        if (ActiveBoard_FindOpenNeighbor(resource.pos, out Axial openNeighbor))
                        {
                            if (TryPlaceCard_FromHand(_turnPlayerIndex, cardIndex, openNeighbor))
                            {
                                GD.Print($"Player {_turnPlayerIndex} placing card {card.NAME} next to resource {resource}");
                                HandleAttackAction(false, card.HP, Axial.Empty, resource);
                                canPlaceOffenseUnit = true;
                                return true;
                            }
                            break;
                        }
                    }

                    if (!canPlaceOffenseUnit)
                        GD.Print($"Player {_turnPlayerIndex} could not place offense unit because existing friendly units had no vacant neighbors OR there was a failure to place the card.");
                }
                else
                {
                    GD.Print($"Could not place offense unit because there are no friendly non-offense units on the board.");
                }
            }
            // Else, get a random open tile to place the unit
            else if (TryGetOpenTile(out Axial openTile))
            {
                TryPlaceCard_FromHand(_turnPlayerIndex, cardIndex, openTile);
            }
            else
                GD.Print("Could not place card because all tiles are filled");

            return false;
        }

        private bool TryEndTurn()
        {
            PostAction(OnTurnEnd, _turnCounter, _turnPlayerIndex);
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
                playerBase = _ActiveBoard[baseIndex];
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
        /// 'out' index within <see cref="_ActiveBoard"/> of the unit at the axial position, if one exists
        /// </summary>
        /// <returns>True if a unit exists at the parameter axial, false if not</returns>
        public bool ActiveBoard_IsAxialOccupied(Axial axial, out int boardIndex)
        {
            for (int i = 0; i < _ActiveBoard.Length; i++)
            {
                Unit unit = _ActiveBoard[i];
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
        /// <remarks>Used in <see cref="Unit_TryRandomAttack"/></remarks>
        private bool ActiveBoard_FindEnemyNeighbor(int ownerIndex, Axial originAxial, out Unit enemyNeighborUnit)
        {
            int[] neighborBoardIndexes;
            if(ActiveBoard_FindNeighbors(originAxial, out neighborBoardIndexes)){
                foreach(int index in neighborBoardIndexes){
                    Unit neighborUnit = _ActiveBoard[index];
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
        private bool ActiveBoard_FindFriendlyNonOffenseNeighbor(int ownerIndex, Axial axial, out Unit friendlyUnit)
        {
            int[] neighborBoardIndexes;
            if(ActiveBoard_FindNeighbors(axial, out neighborBoardIndexes)){
                foreach(int index in neighborBoardIndexes){
                    Unit neighbor = _ActiveBoard[index];
                    if(neighbor.ownerIndex == ownerIndex && neighbor.type != Card.CardType.Offense){
                        friendlyUnit = neighbor;
                        return true;
                    }
                }
            }

            friendlyUnit = Unit.EMPTY;
            return false;
        }

        private bool ActiveBoard_AllFriendlyUnits(int ownerIndex, out Unit[] friendlyUnits)
        {
            List<Unit> friendlyUnits_List = new List<Unit>(0);

            foreach(Unit unit in _ActiveBoard)
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
        private bool ActiveBoard_AllNonOffenseFriendlyUnits(int ownerIndex, out Unit[] friendlyUnits_nonOf)
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

        private bool ActiveBoard_FindFriendlyNonOffenseUnit(int ownerIndex, out Unit friendlyUnit_nonOf)
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

        private bool ActiveBoard_FindOpenNeighbor(Axial origin, out Axial openPos)
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

        private bool IsRoundOver(int turnCounter){

            for(int i = 0; i < _PLAYER_COUNT; i++){
                if(ActiveBoard_TryGetBaseUnit(i, out Unit playerBase)){
                    if(playerBase.hp <= 0){
                        return true;
                    }
                }
            }
            
            return turnCounter >= 100;
        }

        private void DisplayCurrentActiveBoard()
        {
            GD.Print("--- Displaying active board ---");
            for (int i = 0; i < _ActiveBoard.Length; i++)
            {
                Unit unit = _ActiveBoard[i];
                GD.Print($"[{i}]:{unit}");
            }
            GD.Print("-------------------------------");
        }

        private bool Unit_TryRandomMove(Unit unit, out Axial newPos){

            Axial initPos = unit.pos;
            newPos = initPos;

            int iDirection = 0;

            while(unit.CanMove(out int remainingMovement)){
                Thread.Sleep(_waitTime);

                if(iDirection >= Axial.CARDINAL_LENGTH)
                {
                    GD.Print($"Player {unit.ownerIndex} can't move this unit anymore because there are no valid directions to move in.");
                    return (newPos != initPos);
                }

                Axial randDirection = Axial.Direction((Axial.Cardinal)iDirection);
                Axial movePosition = unit.pos + randDirection;
                
                if(Unit_TryMove(true, unit, movePosition)){
                    newPos = movePosition;
                }

                iDirection++;
            }

            return (newPos != initPos);
        }

        public bool Unit_TryMove(bool isWillful, int playerIndex, Axial unitPos, Axial destination)
        {
            if(playerIndex == _turnPlayerIndex)
            {
                if(ActiveBoard_IsAxialOccupied(unitPos, out int boardIndex))
                {
                    Unit unit = _ActiveBoard[boardIndex];

                    return Unit_TryMove(isWillful, unit, destination);
                }
            else
            {
                GD.PrintErr($"Cannot move unit because no unit exists at {unitPos}");
                return false;
            }
            }
            else
            {
                GD.PrintErr("Cannot move unit because it is not this player's turn");
                return false;
            }
        }

        private bool Unit_TryMove(bool isWillful, Unit unit, Axial newPos)
        {
            return Unit_TryMove(isWillful, unit, newPos, out Unit dummyOccupant);
        }

        private bool Unit_TryMove(bool isWillful, Unit unit, Axial newPos, out Unit occupant)
        {
            if (IsPlacementLocationValid(newPos, out occupant))
            {
                if (unit.CanMove(out int remainingMove))
                {
                    GD.Print($"Attempting to move unit {unit.name} from {unit.pos} to {newPos}");

                    int displacement = Axial.Distance(unit.pos, newPos);
                    if (!isWillful || displacement <= remainingMove)
                    {
                        Axial oldPos = unit.pos;
                        unit.Move(isWillful, newPos);
                        PostAction(OnUnitMove, oldPos, unit);
                        return true;
                    }
                    else
                    {
                        GD.Print($"Unit {unit.name} cannot move because the displacement ({displacement}) is greater than the remaining movement ({remainingMove}) AND the movement is willful ({isWillful})");
                        return false;
                    }
                }
                else
                {
                    GD.Print($"Unit {unit.name} cannot move, according to unit data.");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool Unit_TryRandomAttack(Unit unit)
        {
            if(unit.CanAttack())
            {                
                if(ActiveBoard_FindEnemyNeighbor(unit.ownerIndex, unit.pos, out Unit target)){
                    Axial attackDirection = target.pos - unit.pos;
                    return HandleAttackAction(true, unit, attackDirection, target);
                }
            }

            GD.Print($"Player {unit.ownerIndex} can't attack with this unit.");
            return false;
        }

        private bool HandleAttackAction(bool doDisplace, Unit attacker, Axial attackDirection, Unit target)
        {
            GD.Print($"Player {attacker.ownerIndex} attacked {target.name} @ {target.pos} in direction {attackDirection}.");
            
            if(Axial.Distance(Axial.Zero, attackDirection) <= Unit.ATK_RANGE)
            {
                attacker.Attack();
                PostAction(OnUnitAttack, attacker, target);

                return HandleAttackAction(doDisplace, attacker.atk, attackDirection, target);
            }
            else{
                GD.PrintErr($"Could not attack because attack distance ({Axial.Distance(Axial.Zero, attackDirection)}) is greater than 1.");
                return false;
            }
        }

        private bool HandleAttackAction(bool doDisplace, int damage, Axial attackDirection, Unit target)
        {
            PostAction(OnDamaged, target);
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
                Thread.Sleep(_waitTime);

                Axial attackDisplacement = target.pos + attackDirection;
                if (Unit_TryMove(false, target, attackDisplacement, out Unit occupant))
                {
                    GD.Print($"The attack successfully displaced the target.");
                }
                else if (occupant != Unit.EMPTY)
                {
                    Thread.Sleep(250);

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

        private bool TryGetOpenTile(out Axial Axial)
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

        private void ResetAllActiveUnitsTurnActions()
        {
            foreach(Unit unit in _ActiveBoard){
                unit.ResetTurnActions();
            }
        }

        private void DisplayHand(int player_index){

            Thread.Sleep(100);

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

            if(refDrawnCount < deckCount){
                Card drawnCard = iDeck[refDrawnCount];

                int heldCount = AddCardToHand(player_index, drawnCard);

                refDrawnCount++;

                PostAction(OnCardDrawn, player_index, drawnCard.NAME, _Hands[player_index], refDrawnCount);

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
            ref Card[] refHand = ref _Hands[player_index];

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

                PostAction(OnCardRemoved, player_index, newHand);

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
        public bool TryPlaceCard_FromHand(int player_index, int card_index, Axial location)
        {
            ref Card[] refHand = ref _Hands[player_index];

            if(IsPlacementLocationValid(location) && OffensePlacementRule(player_index, location, card_index)){

                Card card = RemoveCardFromHand(player_index, card_index);
                Unit Unit = new Unit(player_index, location, card);

                ActiveBoard_AddUnit(Unit);

                return true;
            }
            else
                return false;
        }

        private bool CheckOffensePlacementRule(int player_index, Axial placement, Card card)
        {
            if(card.TYPE == Card.CardType.Offense){
                if(ActiveBoard_FindFriendlyNonOffenseNeighbor(player_index, placement, out Unit friendlyUnit))
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
            ref Card[] refHand = ref _Hands[player_index];
            Card card = refHand[card_index];

            return CheckOffensePlacementRule(player_index, placement, card);
        }

        private bool TryPlaceCard_FromVoid(int player_index, Axial location, Card card){

            if(IsPlacementLocationValid(location) && CheckOffensePlacementRule(player_index, location, card)){
                Unit unit = new Unit(player_index, location, card);

                ActiveBoard_AddUnit(unit);

                return true;
            }
            else
                return false;
        }

        private void ActiveBoard_AddUnit(Unit newUnit)
        {
            if (_ActiveBoard != null)
            {
                Unit[] newActiveBoard = new Unit[_ActiveBoard.Length + 1];
                _ActiveBoard.CopyTo(newActiveBoard, 0);
                newActiveBoard[newActiveBoard.Length - 1] = newUnit;

                _ActiveBoard = newActiveBoard;
                PostAction(OnUnitAddedToBoard, newUnit);
            }
            else
            {
                _ActiveBoard = new Unit[1] { newUnit };
            }
        }

        private bool ActiveBoard_RemoveUnit(Unit unit, out Unit removedUnit){

            int unit_BoardIndex = -1;

            for (int i = 0; i < _ActiveBoard.Length; i++)
            {
                if(unit == _ActiveBoard[i]){
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
            Unit unitToRemove = _ActiveBoard[unitToRemove_index];
            Unit[] newActiveBoard = new Unit[_ActiveBoard.Length - 1];

            lock (activeBoardLock){

                for (int i = 0, j = 0; i < _ActiveBoard.Length; i++)
                {
                    if (i != unitToRemove_index)
                    {
                        newActiveBoard[j++] = _ActiveBoard[i];
                    }
                }

                _ActiveBoard = newActiveBoard;
            }

            foreach(Unit unit in _ActiveBoard)
            {
                if(unit == unitToRemove){
                    GD.PrintErr($"Failed to remove {unitToRemove} from active board");
                    removedUnit = Unit.EMPTY;
                    return false;
                }
            }

            removedUnit = unitToRemove;
            GD.Print($"Successfully removed {removedUnit.name} from the board");
            PostAction(OnDeath, removedUnit);
            return true;
        }

        public bool IsPlacementLocationValid(Axial location)
        {
            return IsPlacementLocationValid(location, out Unit dummyUnit);
        }

        private bool IsPlacementLocationValid(Axial location, out Unit occupant)
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
                occupant = _ActiveBoard[boardIndex];
                return false;
            }

            return true;
        }

        #endregion
    }
}