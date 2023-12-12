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

        int _myPlayerIndex = 1;

        public Controller_AI(Model_Game model)
        {
            this.model = model;
            model.OnAwaitDrawCard += OnAwaitDrawCard;
            model.OnAwaitTurnActions += OnAwaitTurnActions;
        }

        private void OnAwaitDrawCard(int playerIndex, int drawIndex)
        {
            if (playerIndex == _myPlayerIndex)
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

        Random _random = new Random();

        private void OnAwaitTurnActions(int playerIndex, int turnCounter)
        {
            if (playerIndex == _myPlayerIndex)
            {
                var async = async () =>
                {
                    await System.Threading.Tasks.Task.Delay(50);

                    // --- PLACEMENT ---
                    if (model.EnemyHand.Length > 0)
                    {
                        // Get a max placements which is at least 1, but otherwise half the amount of cards in hand
                        int maxPlacements = Math.Min(1, (int)(model.EnemyHand.Length * 0.75f));

                        int placements = _random.Next(1, maxPlacements);

                        for (int i = 0; i < placements; i++)
                        {
                            int syntheticWait = _random.Next(300, 1200);

                            await System.Threading.Tasks.Task.Delay(syntheticWait);
                            if(TryPlaceRandomCardRandomly())
                            {
                                // On success
                            }
                        }
                    }
                    else
                    {
                        int syntheticWait = _random.Next(250, 500);

                        await System.Threading.Tasks.Task.Delay(syntheticWait);
                    }

                    // --- MOVEMENT ---
                    await System.Threading.Tasks.Task.Delay(150);

                    foreach (Unit occupant in model.ActiveBoard)
                    {
                        int syntheticWait = _random.Next(250, 500);

                        if (occupant.ownerIndex == model.TurnPlayerIndex)
                        {
                            await System.Threading.Tasks.Task.Delay(syntheticWait);

                            Unit iUnit = occupant;

                            Axial oldPos = iUnit.pos;

                            if (Unit_TryRandomMove(iUnit, out Axial newPos))
                            {
                                // On success
                            }
                        }
                    }

                    // --- ATTACK ---
                    await System.Threading.Tasks.Task.Delay(150);

                    if (model.ActiveBoard_AllFriendlyUnits(_myPlayerIndex, out Unit[] AllFriendlyUnits))
                    {
                        foreach (Unit occupant in AllFriendlyUnits)
                        {
                            int syntheticWait = _random.Next(250, 500);

                            if (occupant.ownerIndex == model.TurnPlayerIndex && occupant.type != Card.CardType.Resource)
                            {
                                await System.Threading.Tasks.Task.Delay(syntheticWait);

                                Unit iUnit = occupant;

                                GD.Print($"Player {model.TurnPlayerIndex} attempting to attack with {iUnit.name} at {iUnit.pos}.");

                                if (Unit_TryRandomAttack(iUnit))
                                {
                                    // On success
                                    GD.Print($"Player {model.TurnPlayerIndex} made an attack with {iUnit.name} from {iUnit.pos}.");
                                }
                                else
                                {
                                    GD.Print($"Player {model.TurnPlayerIndex} could not attack with {iUnit.name}");
                                }
                            }
                        }
                    }

                    await System.Threading.Tasks.Task.Delay(1000);
                    model.TriggerEndTurn = true;
                };
                
                async.Invoke();
            }
        }

        private bool TryPlaceRandomCardRandomly()
        {
            if (model.EnemyHand.Length > 0)
            {
                int cardIndex = _random.Next(0,model.EnemyHand.Length);

                return TryPlaceCardRandomly(cardIndex);
            }

            return false;
        }
        
        private bool TryPlaceCardRandomly(int cardIndex)
        {
            Card card = model.EnemyHand[cardIndex];

            // Place based on offense rules
            if (card.TYPE == Card.CardType.Offense)
            {
                if (model.ActiveBoard_AllNonOffenseFriendlyUnits(_myPlayerIndex, out Unit[] resourceUnits))
                {
                    foreach (Unit resource in resourceUnits)
                    {
                        if (model.ActiveBoard_FindOpenNeighbor(resource.pos, out Axial openNeighbor))
                        {
                            if(model.TryPlaceCard_FromHand(_myPlayerIndex, cardIndex, openNeighbor))
                                return true;
                        }
                    }
                }
                else
                {
                    GD.Print($"Could not place offense unit because there are no friendly non-offense units on the board.");
                }
            }
            // Else, get a random open tile to place the unit
            else if (model.ActiveBoard_TryGetOpenTile(out Axial openTile))
            {
                return model.TryPlaceCard_FromHand(_myPlayerIndex, cardIndex, openTile);
            }
            else
                GD.Print("Could not place card because all tiles are filled");

            return false;
        }
        

        public bool Unit_TryRandomMove(Unit unit, out Axial newPos){

            Axial initPos = unit.pos;
            newPos = initPos;

            int iDirection = 0;

            while(unit.HasMovement(out int remainingMovement)){
                if(iDirection >= Axial.CARDINAL_LENGTH)
                {
                    GD.Print($"Player {unit.ownerIndex} can't move this unit anymore because there are no valid directions to move in.");
                    return (newPos != initPos);
                }

                Axial randDirection = Axial.Direction((Axial.Cardinal)iDirection);
                Axial movePosition = unit.pos + randDirection;
                
                if(model.Unit_TryMove(true, unit, movePosition)){
                    newPos = movePosition;
                }

                iDirection++;
            }

            return (newPos != initPos);
        }


        

        public bool Unit_TryRandomAttack(Unit unit)
        {
            if(unit.CanAttack())
            {                
                if(model.ActiveBoard_FindEnemyNeighbor(unit.ownerIndex, unit.pos, out Unit target)){
                    Axial attackDirection = target.pos - unit.pos;
                    return model.Unit_TryAttack(unit, attackDirection, target);
                }
            }

            GD.Print($"Player {unit.ownerIndex} can't attack with this unit.");
            return false;
        }
    }
}