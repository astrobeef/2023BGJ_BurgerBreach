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
    }
}