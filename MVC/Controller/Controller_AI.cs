using System;
using System.Linq;
using System.Collections.Generic;
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
                        // Get a max placements which is at least 1
                        int maxPlacements = Math.Min(1, (int)(model.EnemyHand.Length * 1.0f));

                        int placements = _random.Next(1, maxPlacements);

                        for (int i = 0; i < placements; i++)
                        {
                            int syntheticWait = _random.Next(300, 1200);

                            await System.Threading.Tasks.Task.Delay(syntheticWait);
                            if (TryPlaceRandomCardRandomly())
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

                    GD.Print($"AI beginning move phase...");

                    // --- MOVEMENT ---
                    await System.Threading.Tasks.Task.Delay(150);

                    foreach (Unit occupant in model.ActiveBoard)
                    {
                        if (occupant.ownerIndex == model.TurnPlayerIndex)
                        {
                            int syntheticWait = _random.Next(250, 500);

                            await System.Threading.Tasks.Task.Delay(syntheticWait);

                            Unit iUnit = occupant;

                            GD.Print($"Player {model.TurnPlayerIndex} trying to move {iUnit.name}...");

                            if (Unit_TryRandomMove(iUnit, out Axial newPos))
                            {
                                // On success
                                GD.Print($"Player {model.TurnPlayerIndex} moved {iUnit.name} to {iUnit.pos}.");
                            }
                            else
                            {
                                GD.Print($"Player {model.TurnPlayerIndex} failed to move {iUnit.name}");
                            }
                        }
                    }

                    GD.Print($"AI beginning attack phase...");

                    // --- ATTACK ---
                    await System.Threading.Tasks.Task.Delay(150);

                    if (model.ActiveBoard_AllFriendlyUnits(_myPlayerIndex, out Unit[] AllFriendlyUnits))
                    {
                        foreach (Unit occupant in AllFriendlyUnits)
                        {
                            int syntheticWait = _random.Next(250, 500);

                            if (occupant != null && occupant.ownerIndex == model.TurnPlayerIndex && occupant.type != Card.CardType.Resource)
                            {
                                await System.Threading.Tasks.Task.Delay(syntheticWait);

                                GD.Print($"AI attempting to attack with {occupant.name} at {occupant.pos}...");

                                if (Unit_TryPriorityAttack(occupant))
                                {
                                    // On success
                                    GD.Print($"AI made an attack with {occupant.name} from {occupant.pos}.");
                                }
                                else
                                {
                                    GD.Print($"AI could not attack with {occupant.name}");
                                }
                            }
                        }
                    }

                    GD.Print("AI ending turn");

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
                int cardIndex = _random.Next(0, model.EnemyHand.Length);

                return TryPlaceCardRandomly(cardIndex);
            }

            return false;
        }
        
        private string[] placementSourcePriority = new string[]
        {
            Card.BURGER_NAME,
            Card.MOE_FAMILY_FRIES_NAME,
            Card.THE_SCRAPS_NAME,
            Card.CLAM_CHOWDER_NAME,
            Card.BASE_NAME
        };

        private bool TryPlaceCardRandomly(int cardIndex)
        {
            Card card = model.EnemyHand[cardIndex];

            // Place based on offense rules
            if (card.TYPE == Card.CardType.Offense)
            {
                if (model.GetAllValidOffensePlacements(_myPlayerIndex, card, out Dictionary<Axial,Unit> validPlacements))
                {
                    KeyValuePair<Axial, Unit>[] sortedList = validPlacements
                        .OrderBy(pair => Array.IndexOf(placementSourcePriority, pair.Value.card.NAME))
                        .ToArray();

                    if (sortedList != null && sortedList.Length > 0)
                    {
                        Axial placement = Axial.Empty;

                        foreach(KeyValuePair<Axial, Unit> pair in sortedList)
                        {
                            Axial place = pair.Key;
                            Unit resource = pair.Value;

                            //Do not chose base if it has low HP OR if the source is The Scraps and this card has 1 base HP
                            if(resource.card.NAME == Card.BASE_NAME && resource.hp < resource.card.HP / 2
                            || resource.card.NAME == Card.THE_SCRAPS_NAME && card.HP == 1)
                            {
                                continue;
                            }
                            else
                            {
                                placement = pair.Key;
                                break;
                            }
                        }

                        //If we could not avoid using the base
                        if(placement == Axial.Empty)
                        {
                            GD.Print($"AI chose not to place {card}@{placement} because it would have had to use the base, which has low HP");
                            return false;
                        }

                        //Place card at first valid placement
                        if (model.TryPlaceCard_FromHand(_myPlayerIndex, cardIndex, placement))
                        {
                            GD.Print($"AI placed card {card}@{placement} from hand");
                        }
                        else GD.Print($"AI could not place card from hand");
                    }
                    else GD.Print($"AI could not place card from hand");
                }
                else
                {
                    GD.Print($"AI could not place {card} because there were no valid placement options");
                }
            }
            // Else, get a random open tile to place the resource unit
            else
            {
                if (model.GetAllOpenResourcePlacements(_myPlayerIndex, out Axial[] validResourcePlacements))
                {
                    Axial placement = validResourcePlacements[0];
                    //Place card at first valid placement
                    if (model.TryPlaceCard_FromHand(_myPlayerIndex, cardIndex, validResourcePlacements[0]))
                    {
                        GD.Print($"AI placed card {card}@{placement} from hand");
                    }
                    else GD.Print($"AI could not place card from hand");
                }
                else
                {
                    GD.Print($"AI could not place {card} because there were no valid placement options");
                }
            }

            return false;
        }

        private Axial.Cardinal[] moveDirectionPriority = new Axial.Cardinal[]
        {
            Axial.Cardinal.SW,
            Axial.Cardinal.W,
            Axial.Cardinal.SE,
            Axial.Cardinal.E,
            Axial.Cardinal.NW,
            Axial.Cardinal.NE
        };

        public bool Unit_TryRandomMove(Unit unitToMove, out Axial newPos)
        {
            Axial initPos = unitToMove.pos;
            newPos = initPos;

            if (unitToMove.GetAllMovePositions(out Dictionary<Axial.Cardinal, Axial[]> validMoves))
            {
                Axial chosenMovePosition = GetPriorityMovePosition(unitToMove, validMoves);

                GD.Print($"AI trying to move unit {unitToMove.name}@{unitToMove.pos} towards priority position {chosenMovePosition}");

                if (model.Unit_TryMove(true, unitToMove, chosenMovePosition))
                {
                    GD.Print($"AI moved unit {unitToMove.name}@{unitToMove.pos} towards priority position {chosenMovePosition}");
                    newPos = unitToMove.pos;
                }
                else
                {

                    GD.Print($"AI failed to move unit {unitToMove.name}@{unitToMove.pos} towards priority position {chosenMovePosition}");
                }
            }
            else
            {
                GD.Print($"AI could not move {unitToMove.name} because all move positions returned empty OR this unit cannot move");
            }

            return newPos != initPos;
        }

        private Axial GetPriorityMovePosition(Unit unitToMove, Dictionary<Axial.Cardinal, Axial[]> validMoves)
        {
            Unit priorityTarget = GetPriorityAttackTarget_Global(unitToMove);
            
            GD.Print($"For movemenet, got global priority target {priorityTarget}");

            if (priorityTarget != null)
            {
                if (Axial.Distance(priorityTarget.pos, unitToMove.pos) <= unitToMove.atk_range)
                {
                    //We are next to our priority target, so don't move
                    return unitToMove.pos;
                }

                //Try to move towards priority target
                Axial directionTowardsPriority = Axial.ApproximateCardinal(priorityTarget.pos - unitToMove.pos);
                if (Axial.isCardinal(directionTowardsPriority, out Axial.Cardinal cardinal))
                {
                    if(validMoves.ContainsKey(cardinal))
                    {
                        //If the direction towards the priority target is available
                        if (validMoves[cardinal].Length > 0)
                        {
                            Axial[] chosenMoveDirection = validMoves[cardinal];
                            //Chose the furtherst move in the direction chosen
                            Axial chosenMovePosition = chosenMoveDirection[chosenMoveDirection.Length - 1];
                            return chosenMovePosition;
                        }
                    }
                    else
                    {
                        GD.PrintErr($"validMoves is missing cardinal key {cardinal}. validMoves should have all keys even if there are no values");
                        return unitToMove.pos;
                    }
                }
            }

            {
                // Find the first non-null and non-empty move
                Axial[] chosenMoveDirection = moveDirectionPriority
                    .Select(direction => validMoves[direction])
                    .FirstOrDefault(moves => moves != null && moves.Length > 0);

                //Chose the furtherst move in the direction chosen
                if (chosenMoveDirection != null && chosenMoveDirection.Length > 0)
                {
                    Axial chosenMovePosition = chosenMoveDirection[chosenMoveDirection.Length - 1];
                    return chosenMovePosition;
                }
                else
                {
                    string error = $"Could not find move direction for {unitToMove}, but this method should not be called if no movement options exist.";
                    GD.PrintErr(error);
                    return unitToMove.pos;
                }
            }
        }

        private string[] targetPriority_Enemy = new string[]
        {
            Card.CLAM_CHOWDER_NAME,
            Card.BURGER_NAME,
            Card.BIG_MOE_NAME,
            Card.BUSSER_RACOON_NAME,
            Card.LINE_SQUIRREL_NAME,
            Card.MOE_FAMILY_FRIES_NAME,
            Card.BASE_NAME,
            Card.EXPO_PIGEON_NAME,
            Card.THE_SCRAPS_NAME
        };

        private string[] targetPriority_Friendly = new string[]
        {
            Card.BURGER_NAME
        };

        private Unit GetPriorityAttackTarget_Global(Unit attackingUnit)
        {
            Unit[] units = model.ActiveBoard;
            if (units.Length == 0 || attackingUnit == null)
                return null;

            Unit priorityTarget;

            // Try to get a friendly priority (only burgers at the moment)
            Unit priorityTarget_Friendly = units
                .Where(unit => unit != null && unit != attackingUnit && targetPriority_Friendly.Contains(unit.card.NAME)) // Filter out null units and those not in targetPriority
                .OrderBy(unit => Array.IndexOf(targetPriority_Friendly, unit.card.NAME)) // Order by priority
                .FirstOrDefault(); // Take the first unit
                
            // If could not find a friendly unit
            if (priorityTarget_Friendly == null)
            {
                priorityTarget = units
                    .Where(unit => unit != null && unit != attackingUnit && targetPriority_Enemy.Contains(unit.card.NAME) && unit.ownerIndex != _myPlayerIndex) // Filter out null units and those not in targetPriority
                    .OrderBy(unit => Array.IndexOf(targetPriority_Enemy, unit.card.NAME)) // Order by priority
                    .FirstOrDefault(); // Take the first unit
            }
            else priorityTarget = priorityTarget_Friendly;

            if (priorityTarget == null)
            {
                GD.PrintErr($"When filtering for priority attack target (global), the attacking unit came back null. This is likely an error with the LINQ or with the priority list (may be missing a card type)");
                return null;
            }
            else return priorityTarget;
        }

        private bool Unit_TryPriorityAttack(Unit attackingUnit)
        {
            Unit priorityTarget = GetPriorityAttackTarget(attackingUnit);

            if (priorityTarget != null && attackingUnit != null)
            {
                Axial attackDirection = priorityTarget.pos - attackingUnit.pos;
                return model.Unit_TryAttack(attackingUnit, attackDirection, priorityTarget);
            }
            else
                return false;
        }

        private Unit GetPriorityAttackTarget(Unit attackingUnit)
        {
            if (attackingUnit.GetAllAttackTargets(out Unit[] validTargets))
            {
                Unit priorityTarget;

                // Try to get a friendly priority (only burgers at the moment)
                Unit priorityTarget_Friendly = validTargets
                    .Where(unit => unit != null && unit != attackingUnit && targetPriority_Friendly.Contains(unit.card.NAME)) // Filter out null units and those not in targetPriority
                    .OrderBy(unit => Array.IndexOf(targetPriority_Friendly, unit.card.NAME)) // Order by priority
                    .FirstOrDefault(); // Take the first unit

                // If could not find a friendly unit
                if (priorityTarget_Friendly == null)
                {
                    priorityTarget = validTargets
                        .Where(unit => unit != null && unit != attackingUnit && targetPriority_Enemy.Contains(unit.card.NAME) && unit.ownerIndex != _myPlayerIndex) // Filter out null units and those not in targetPriority and friendly units
                        .OrderBy(unit => Array.IndexOf(targetPriority_Enemy, unit.card.NAME)) // Order by priority
                        .FirstOrDefault(); // Take the first unit
                }
                else priorityTarget = priorityTarget_Friendly;

                if (priorityTarget == null)
                {
                    GD.Print($"When filtering for priority attack target, the attacking unit came back null");
                    return null;
                }
                else return priorityTarget;
            }
            else
            {
                GD.Print($"When filtering for priority attack target, GetAllAttackTargets can back null");
                return null;
            }
        }


        private bool Unit_TryRandomAttack(Unit unit)
        {
            if (unit.GetAllAttackTargets(out Unit[] validTargets))
            {
                Unit target = validTargets[0];
                Axial attackDirection = target.pos - unit.pos;
                return model.Unit_TryAttack(unit, attackDirection, target);
            }

            GD.Print($"Player {unit.ownerIndex} can't attack with this unit.");
            return false;
        }
    }
}