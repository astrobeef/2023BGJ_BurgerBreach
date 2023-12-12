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
            model.OnUnitBuffed += OnUnitBuffed;
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

            main.Instance.Player.OnCamClickNewHit += OnCamClickNewHit;
            main.Instance.Player.OnCamClickOff += OnCamClickOff;
            main.Instance.Player.OnCamClickUpdate += OnCamClickUpdate;
        }

        private void OnGameStart(Card[] CardSet, Card[] CardSet_NoBases)
        {
        }

        private void OnRoundStart(int roundCounter)
        {
        }

        private void OnTurnStart(int turnPlayerIndex, int turnCounter)
        {
        }

        private void OnTurnEnd(int playerIndex, int turnIndex)
        {
        }
        private void OnDeckBuildStart(int ownerIndex)
        {
        }

        private void OnDeckBuildAddedCard(int ownerIndex, int cardIndexInDeck, Card card)
        {
        }

        private void OnDeckBuildFinished(int ownerIndex)
        {
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

        private void OnUnitDamaged(Unit attacker, Unit target)
        {
            Print($"Unit ${target?.name} was damaged by {attacker?.name}!");
        }
        
        private void OnUnitBuffed(Unit unit)
        {
            Print($"Unit ${unit.name} was buffed!");
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
            // Print($"Player {ownerIndex} cannot draw any more cards because either their drawn count ({cardsDrawn}) is equal to their deck count ({deckCount})");
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
                playerInput_1.Pressed += HandleInput_EndTurn;
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



        private void OnCamHoverNewHit(Hit3D hit)
        {
            // Print($"Cam hover new object: {hit}");
        }

        private void OnCamHoverOff(Hit3D hit)
        {
            // Print($"Cam no longer hovering: {hit}");
        }

        private void OnCamHoverUpdate(Hit3D hit)
        {
            // Print($"Cam hover update hit position: {hit.position}");
        }



        private void OnCamClickNewHit(Hit3D hit)
        {
            Print($"Cam click new object: {hit}");
        }

        private void OnCamClickOff(Hit3D hit)
        {
            Print($"Cam no longer clicked on: {hit}");
        }

        private void OnCamClickUpdate(Hit3D hit)
        {
            Print($"Cam click update hit position: {hit.position}");
        }

        #endregion

        bool tryReadyAgain = false;

    }
}