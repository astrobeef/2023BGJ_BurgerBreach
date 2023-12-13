using System;
using System.Runtime.InteropServices;
using AxialCS;
using Godot;
using static Model.ActionPoster;

namespace Model
{
    public partial class Unit_BusserRacoon : Unit
    {
        public Unit_BusserRacoon(int ownerIndex, Axial position, Card card) : base(ownerIndex, position, card)
        {
        }

        protected override bool Attack(Unit target)
        {
            if(base.Attack(target))
            {
                if(target.hp <= 0)
                {
                    
                    GD.Print($"Racoon tried to displace {target.name}@{target.pos}, but the unit is dead.");
                    return true;
                }

                Axial targetPos = target.pos;

                Axial attackDirection = targetPos - this.pos;
                Axial inverseDirection = Axial.Zero - attackDirection;
                Axial attackDisplacement = targetPos + (2 * inverseDirection);

                if (target.type == Card.CardType.Resource)
                {
                    if (main.Instance.gameModel.Unit_TryMove(false, target, attackDisplacement))
                    {
                        GD.Print($"Racoon attack on {target.name}@{targetPos} resulted in displacement to {target.pos}");

                        if(Model_Game.PLAYER_COUNT == 2)
                        {
                            int newOwnerIndex = 1 - target.ownerIndex;
                            target.ownerIndex = newOwnerIndex;
                            return PostAction(main.Instance.gameModel.OnUnitOwnerChanged, target);
                        }
                        else
                        {
                            GD.PrintErr("Attack not set up for more than 2 players");
                            return false;
                        }
                    }
                    else
                    {
                        GD.Print($"Racoon tried to displace {target.name}@{targetPos} to {attackDisplacement}, but the unit could not move (tile is likely occupied)");
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
    }
}