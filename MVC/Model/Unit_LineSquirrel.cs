using System;
using System.Runtime.InteropServices;
using AxialCS;
using Godot;

namespace Model
{
    public partial class Unit_LineSquirrel : Unit
    {
        public Unit_LineSquirrel(int ownerIndex, Axial position, Card card) : base(ownerIndex, position, card)
        {
        }

        public override bool CanAttack(Unit target)
        {
            if(base.CanAttack(target))
            {
                // Check that there are no units in the way
                Axial targetPos = target.pos;
                int distance = Axial.Distance(targetPos, this.pos);

                if(distance > 1)
                {
                    Axial attackDirection = targetPos - this.pos;
                    Axial unitAttackDirection = Axial.ApproximateCardinal(attackDirection);

                    Axial expectedTargetPos = this.pos + (unitAttackDirection * distance);

                    // Ensure that the attack direction is in a direct cardinal direction (straight line from this unit to target)
                    if(expectedTargetPos == targetPos)
                    {
                        // Check each Axial along the path to the target
                        for(int i = 1; i < distance; i++)
                        {
                            Axial nextTravelPosition = this.pos + (i * unitAttackDirection);

                            // Check if the Axial along the path towards the target is occupied
                            if(main.Instance.gameModel.ActiveBoard_IsAxialOccupied(nextTravelPosition, out int activeBoardIndex))
                            {
                                Unit unitInPath = main.Instance.gameModel.ActiveBoard[activeBoardIndex];
                                
                                if(unitInPath != target)
                                {
                                    GD.Print($"Squirrel Unit {this.name}@{this.pos} could not attack target {target.name}@{target.pos} because unit ({unitInPath.name}@{unitInPath.pos}) is in the way.");
                                    return false;
                                }
                                else
                                {
                                    GD.PrintErr($"Squirrel unit check somehow detected the unit it is trying to attack. This should not be possible.");
                                    return false;
                                }
                            }
                        }

                        // If we made it this far, then no unit is in the way
                        GD.Print($"Squirrel Unit {this.name}@{this.pos} can attack {target.name}@{target.pos}.");
                        return true;
                    }
                    else
                    {
                        GD.Print($"Squirrel Unit {this.name}@{this.pos} could not attack target {target.name}@{target.pos} because the shot would not be straight.");
                        return false;
                    }
                }
                else return true;
            }
            else return false;
        }
    }
}