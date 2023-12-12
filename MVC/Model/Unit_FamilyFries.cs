using System;
using System.Runtime.InteropServices;
using AxialCS;
using Godot;

namespace Model
{
    public partial class Unit_FamilyFries : Unit
    {
        public Unit_FamilyFries(int ownerIndex, Axial position, Card card) : base(ownerIndex, position, card)
        {
        }
    }
}