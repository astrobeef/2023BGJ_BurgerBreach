using System;
using System.Runtime.InteropServices;
using AxialCS;
using Godot;

namespace Model
{
    public partial class Unit_TheScraps : Unit
    {
        private const int _THORNS_DAMAGE = 1;

        public Unit_TheScraps(int ownerIndex, Axial position, Card card) : base(ownerIndex, position, card)
        {
        }
        

        protected override void OnUnitDamaged(Unit attacker, Unit target)
        {
            if (target == this)
            {
                base.OnUnitDamaged(attacker, target);

                if(attacker != null)
                {
                    if(attacker.TryDamage(this, _THORNS_DAMAGE, attacker)){
                        GD.Print($"Scraps unit @ {this.pos} reacted to the attacker {attacker.name}@{attacker.pos} by damaging it with {_THORNS_DAMAGE} damage");
                    }
                }
            }
        }
    }
}