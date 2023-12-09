using System;
using AxialCS;
using Godot;
using Model;
using View;

namespace Controller.AI
{
    public class Controller_AI
    {
        Model_Game model;

        public Controller_AI(Model_Game model)
        {
            this.model = model;
            model.OnAwaitDrawCard += OnAwaitDrawCard;
            model.OnAwaitTurnActions += OnAwaitTurnActions;
        }

        private void OnAwaitDrawCard(int playerIndex, int drawIndex)
        {
            if (playerIndex != 0)
            {
                GD.Print("AI drawing card");
                var async = async () =>
                {
                    await System.Threading.Tasks.Task.Delay(500);
                    model.TriggerDrawCard = true;
                };
                async.Invoke();
            }
        }

        Random random = new Random();

        private void OnAwaitTurnActions(int playerIndex, int turnCounter)
        {
            if (playerIndex != 0)
            {
                GD.Print("AI beginning turn");
                var async = async () =>
                {
                    await System.Threading.Tasks.Task.Delay(50);

                    // --- PLACEMENT ---
                    if (model.EnemyHand.Length > 0)
                    {
                        // Get a max placements which is at least 1, but otherwise half the amount of cards in hand
                        int maxPlacements = Math.Min(1, (int)(model.EnemyHand.Length * 0.75f));

                        int placements = random.Next(1, maxPlacements);

                        GD.Print($"AI will try to place {placements} cards");

                        for (int i = 0; i < placements; i++)
                        {
                            int syntheticWait = random.Next(300, 1200);

                        GD.Print("AI trying to play a random card");
                            await System.Threading.Tasks.Task.Delay(syntheticWait);
                            model.TryPlaceRandomCardRandomly();
                        }
                    }
                    else
                    {
                        GD.Print("AI could not play any cards because their hand is empty");
                        int syntheticWait = random.Next(250, 500);

                        await System.Threading.Tasks.Task.Delay(syntheticWait);
                    }

                    // --- MOVEMENT ---
                    await System.Threading.Tasks.Task.Delay(150);

                    foreach (Unit occupant in model.ActiveBoard)
                    {
                        int syntheticWait = random.Next(250, 500);

                        if (occupant.ownerIndex == model.TurnPlayerIndex)
                        {
                            await System.Threading.Tasks.Task.Delay(syntheticWait);

                            Unit iUnit = occupant;

                            Axial oldPos = iUnit.pos;

                            GD.Print($"Player {model.TurnPlayerIndex} attempting to move {iUnit.name} at {oldPos}.");

                            if (model.Unit_TryRandomMove(iUnit, out Axial newPos))
                            {
                                GD.Print($"Player {model.TurnPlayerIndex} moved {iUnit.name} from {oldPos} to {newPos}.");
                            }
                            else
                            {
                                GD.Print($"Player {model.TurnPlayerIndex} could not move {iUnit.name}");
                            }
                        }
                    }

                    // --- ATTACK ---
                    await System.Threading.Tasks.Task.Delay(150);

                    foreach (Unit occupant in model.ActiveBoard)
                    {
                        int syntheticWait = random.Next(250, 500);

                        if (occupant.ownerIndex == model.TurnPlayerIndex)
                        {
                            await System.Threading.Tasks.Task.Delay(syntheticWait);

                            Unit iUnit = occupant;

                            GD.Print($"Player {model.TurnPlayerIndex} attempting to attack with {iUnit.name} at {iUnit.pos}.");

                            if (model.Unit_TryRandomAttack(iUnit))
                            {
                                GD.Print($"Player {model.TurnPlayerIndex} made an attack with {iUnit.name} from {iUnit.pos}.");
                            }
                            else
                            {
                                GD.Print($"Player {model.TurnPlayerIndex} could not attack with {iUnit.name}");
                            }
                        }
                    }

                    await System.Threading.Tasks.Task.Delay(1000);
                    model.TriggerEndTurn = true;
                };
                
                async.Invoke();
            }
        }
    }
}