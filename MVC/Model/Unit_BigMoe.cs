using System;
using System.Runtime.InteropServices;
using AxialCS;
using Godot;
using static Model.ActionPoster;

namespace Model
{
    public partial class Unit_BigMoe : Unit
    {
        private static readonly int _COLLISION_DAMAGE = 1;

        public Unit_BigMoe(int ownerIndex, Axial position, Card card) : base(ownerIndex, position, card)
        {
        }

        public override bool HasMovement(out int remainingMovement)
        {
            if (!base.HasMovement(out remainingMovement))
                return false;

            return true;
        }

        protected override void OnUnitAddedToBoard(Unit newUnit)
        {
            if(newUnit == this)
            {
                base.OnUnitAddedToBoard(newUnit);
            }
        }

        protected override bool Attack(Unit target)
        {
            if (base.Attack(target))
            {
                Axial attackDirection = target.pos - this.pos;
                Axial attackDisplacement = target.pos + attackDirection;

                if (main.Instance.gameModel.Unit_TryMove(false, target, attackDisplacement, out Unit occupant))
                {
                    GD.Print($"The attack successfully displaced the target.");
                }
                else if (occupant != Unit.EMPTY)
                {
                    GD.Print($"The target could not be displaced because it collided with {occupant}. Damaging both units.");
                    if (target.hp > 0)
                        target.TryDamage(null, _COLLISION_DAMAGE, target);

                    occupant.TryDamage(null, _COLLISION_DAMAGE, occupant);
                    PostAction(main.Instance.gameModel.OnCollision, target, occupant);
                }
                else if (target.hp > 0)
                {
                    GD.Print($"The target could not be displaced because it would have moved off the map. Damaging unit.");

                    target.TryDamage(null, _COLLISION_DAMAGE, target);
                    PostAction(main.Instance.gameModel.OnCollision, target, occupant);
                }

                return true;
            }

            return false;
        }
    }
}