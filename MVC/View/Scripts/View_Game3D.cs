using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AxialCS;
using EditorTools;
using Godot;
using Model;
using Utility;

namespace View
{
    public partial class View_Game3D : Control
    {
        public bool isInit = false;
        public bool isInitializing = false;
        public object initLock = new object();

        Model_Game model;

        #region  INITIALIZATION

        public override void _Ready()
        {
            if (!isInit)
            {
                isInitializing = true;
                
                if (!InitPlayerInput()) GD.PrintErr("Could not initialize player button");
                else Print("Success! Player button initialized!");

                AwaitInitialization();

                // DrawGrid();
            }
        }

        private bool InitPlayerInput()
        {
            playerInput_1 = FindChild("InputButton") as Button;

            if (playerInput_1 == null)
            {
                GD.PrintErr("Could not find InputButton");
                return false;
            }
            else
            {
                return true;
            }
        }

        private async void AwaitInitialization()
        {
            isInit = await HandleModelAssignment();
            isInitializing = false;

            Print("View is ready!");
        }

        private async Task<bool> HandleModelAssignment()
        {
            bool isModelInit = await AssignModel(0);

            if (isModelInit)
            {
                InitEvents();
            }

            return isModelInit;
        }

        private async Task<bool> AssignModel(int failsafe)
        {
            failsafe++;
            if (main.Instance?.gameModel == null)
            {
                if (failsafe < 1000)
                {
                    await Task.Delay(1);
                    return await AssignModel(failsafe);
                }
                else
                {
                    GD.PrintErr("Could not assign model reference.");
                    return false;
                }
            }
            else
            {
                model = main.Instance.gameModel;
                return true;
            }
        }

        #endregion

        private void OnProcess()
        {
        }

        #region Model Driven Events

        private void InitEvents()
        {
            GD.Print("Init View events");

            model.OnGameStart += OnGameStart;
            model.OnRoundStart += OnRoundStart;
            model.OnTurnStart += OnTurnStart;
            model.OnTurnEnd += OnTurnEnd;

            model.OnDeckBuildStart += OnDeckBuildStart;
            model.OnDeckBuildAddedCard += OnDeckBuildAddedCard;
            model.OnDeckBuildFinished += OnDeckBuildFinished;

            model.OnUnitAddedToBoard += OnUnitAddedToBoard;
            model.OnUnitMove += OnUnitMove;
            model.OnUnitAttack += OnUnitAttack;
            model.OnBaseDestroyed += OnBaseDestroyed;
            model.OnUnitDamaged += OnUnitDamaged;
            model.OnUnitDeath += OnDeath;
            model.OnCollision += OnCollision;

            model.OnCardDrawn += OnCardDrawn;
            model.OnCardDrawn_fail += OnCardDrawn_fail;
            model.OnCardRemoved += OnCardRemoved;

            // INPUT
            model.OnAwaitStartGame += OnAwaitStartGame;
            model.OnAwaitDrawCard += OnAwaitDrawCard;

            model.OnAwaitTurnActions += OnAwaitTurnActions;

            // PLAYER INPUT
            main.Instance.Player.OnCamHoverNewHit += OnCamHoverNewHit;
            main.Instance.Player.OnCamHoverOff += OnCamHoverOff;
            main.Instance.Player.OnCamHoverUpdate += OnCamHoverUpdate;
        }

        private void OnGameStart(Card[] CardSet, Card[] CardSet_NoBases)
        {
            Print("Game started!");
            DisplayCardSet(CardSet, CardSet_NoBases);
        }

        private void OnRoundStart(int roundCounter)
        {
            Print("-------------------------");
            Print($"----- START ROUND {roundCounter} -----");
            Print("-------------------------");
        }

        private void OnTurnStart(int turnCounter, int turnPlayerIndex)
        {
            GD.Print("------------------------");
            GD.Print($"----- START TURN {turnCounter + 1} -----");
            GD.Print($"It is player [{turnPlayerIndex}]'s turn");
            GD.Print("------------------------");
        }

        private void OnTurnEnd(int turnCounter, int playerIndex)
        {
            if(model.OnCardRemoved != null)
                model.OnCardRemoved -= SubscribeAllCards;
        }

        private void DisplayCardSet(Card[] CardSet, Card[] CardSet_NoBases)
        {
            Print("----- CARD SET W/ BASES -----");

            for (int i = 0; i < CardSet.Length; i++)
            {
                Card card = CardSet[i];
                Print($"[{i}] = {card}");
            }

            Print("----- CARD SET W/O BASES -----");

            for (int i = 0; i < CardSet_NoBases.Length; i++)
            {
                Card card = CardSet_NoBases[i];
                Print($"[{i}] = {card}");
            }
        }

        private void OnDeckBuildStart(int ownerIndex)
        {
            Print($"----- Initializing deck[{ownerIndex}] -----");
        }

        private void OnDeckBuildAddedCard(int ownerIndex, int cardIndexInDeck, Card card)
        {
            Print($"Deck [{ownerIndex}][{cardIndexInDeck}] : {card}");
        }

        private void OnDeckBuildFinished(int ownerIndex)
        {
            Print($"----- Initialized deck[{ownerIndex}] -----");
            Print("-------------------------------");
        }

        private void OnUnitAddedToBoard(Unit newUnit)
        {
            Print($"Player {newUnit.ownerIndex} placing card {newUnit.name} at location {newUnit.pos}");
        }

        private void OnUnitMove(Axial oldPos, Unit unit)
        {
            Print($"Player {unit.ownerIndex} moved unit {unit.name} from {oldPos} to {unit.pos}. Calculated displacement: {unit.pos - oldPos}.");
        }

        private void OnUnitAttack(Unit attacker, Unit target)
        {
            Print($"Unit ${attacker.name} attacked {target.name}!");
        }

        private void OnUnitDamaged(Unit unit)
        {
            Print($"Unit ${unit.name} was damaged!");
        }

        private void OnBaseDestroyed(Unit Base)
        {
            Print($"GAME OVER!");
        }

        private void OnCollision(Unit mover, Unit occupant)
        {
            Print($"Unit ${mover.name} collided with {occupant.name}!");
        }

        private void OnDeath(Unit unit)
        {
            Print($"Unit ${unit.name} destroyed!");
        }

        private void OnCardDrawn(int ownerIndex, Card card, Card[] heldCards, int cardsDrawn)
        {
            Print($"Player {ownerIndex} drew a card ({card.NAME}), increasing their hand to {heldCards.Length}. Their drawn count has incremented to {cardsDrawn}");
        }

        private void OnCardRemoved(int ownerIndex, Card cardRemoved, Card[] heldCards)
        {
        }

        private void OnCardDrawn_fail(int ownerIndex, int cardsDrawn, int deckCount)
        {
            Print($"Player {ownerIndex} cannot draw any more cards because their drawn count ({cardsDrawn}) is equal to their deck count ({deckCount})");
        }

        private void Print(string message)
        {
            GD.Print(message);
        }

        #endregion

        #region View Driven Actions

        public Button playerInput_1;

        private void OnAwaitStartGame()
        {
            if(playerInput_1 == null)
                return;

            playerInput_1.Text = "Start Game";
            playerInput_1.Pressed += HandleInput_StartGame;
        }

        private void HandleInput_StartGame()
        {
            if(playerInput_1 == null)
                return;

            playerInput_1.Text = "No input Registered";
            model.triggerStartGame = true;
            playerInput_1.Pressed -= HandleInput_StartGame;
        }

        private void OnAwaitDrawCard(int playerIndex, int drawIndex)
        {
            if(playerIndex != 0 || playerInput_1 == null)
                return;

            playerInput_1.Text = "Draw Card";
            playerInput_1.Pressed += HandleInput_DrawCard;
        }

        private void HandleInput_DrawCard()
        {
            if(playerInput_1 == null)
                return;
                
            playerInput_1.Text = "No Input Registered";
            model.TriggerDrawCard = true;
            playerInput_1.Pressed -= HandleInput_DrawCard;
        }

        private void OnAwaitTurnActions(int playerIndex, int turnIndex)
        {
            if (playerIndex == 0)
            {
                playerInput_1.Text = "End Turn";
            }
        }

        private void HandleInput_EndTurn()
        {
            if(playerInput_1 == null)
            return;

            playerInput_1.Text = "No Input Registered";
            model.TriggerEndTurn = true;
            playerInput_1.Pressed -= HandleInput_EndTurn;
        }

        private bool _leftMouseClicked = false;
        private Axial _unitToMove = Axial.Empty;

        private void OnMouseInput_MoveUnitRequest()
        {
            Utility.MouseMoveObserver MouseMoveObserver = main.Instance.MouseMoveObserver;

            if (_leftMouseClicked)
                _leftMouseClicked = Input.IsMouseButtonPressed(MouseButton.Left);

            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                if (!_leftMouseClicked)
                {
                    _leftMouseClicked = true;
                    MouseMoveObserver._pos_cur = GetViewport().GetMousePosition();
                }
            }
        }

        private void SubscribeAllCards(int dummyInt, Card cardRemoved, Card[] dummyHand)
        {
        }

        private void SubscribeCardPressed(Button card, int index)
        {
            card.Pressed -= OnCardPressed;

            card.Pressed += OnCardPressed;

            void OnCardPressed()
            {
                HandleInput_PlaceCard(index);
                card.Pressed -= OnCardPressed;
            }
        }

        private void HandleInput_PlaceCard(int cardIndex)
        {
            model.TryPlaceCardRandomly(0, cardIndex);
        }

        private void OnCamHoverNewHit(Hit3D hit)
        {
            // Print($"Hit new object: {hit}");
        }

        private void OnCamHoverOff(Hit3D hit)
        {
            // Print($"No longer hitting {hit}");
        }

        private void OnCamHoverUpdate(Hit3D hit)
        {
            // Print($"Hit position has changed to: {hit.position}");
        }

        #endregion

        bool tryReadyAgain = false;

    }
}