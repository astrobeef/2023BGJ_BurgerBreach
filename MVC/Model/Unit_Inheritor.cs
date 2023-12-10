using System;
using System.Runtime.InteropServices;
using AxialCS;
using Godot;

namespace Model
{
    public partial class Unit_Inheritor : Unit
    {
        public Unit_Inheritor(int ownerIndex, Axial position, Card card) : base(ownerIndex, position, card)
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
            base.OnUnitAddedToBoard(newUnit);
        }
    }
}