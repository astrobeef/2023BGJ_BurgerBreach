using System;
using System.Runtime.InteropServices;
using AxialCS;
using Godot;

namespace Model
{
    public partial class Unit_Burger : Unit
    {
        private const int _HP_BUFF = 1, _ATK_BUFF = 1; 

        public Unit_Burger(int ownerIndex, Axial position, Card card) : base(ownerIndex, position, card)
        {
        }

        public override bool HasMovement(out int remainingMovement)
        {
            if(!base.HasMovement(out remainingMovement))
                return false;

            return true;
        }

        protected override void OnUnitAddedToBoard(Unit newUnit)
        {
            if (newUnit == this)
            {
                base.OnUnitAddedToBoard(newUnit);
            }
        }

        protected override void OnUnitDamaged(Unit attacker, Unit target)
        {
            if (target == this)
            {
                base.OnUnitDamaged(attacker, target);

                if (attacker != null)
                {
                    if (attacker.TryBuff(true, _HP_BUFF, _ATK_BUFF))
                        GD.Print($"Unit {this.name} buffed {attacker.name}");
                }
            }
        }
    }
}