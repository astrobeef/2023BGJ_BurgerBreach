using System;
using System.Runtime.InteropServices;
using AxialCS;
using Godot;

namespace Model
{
    public partial class Unit_ExpoPigeon : Unit
    {
        public Unit_ExpoPigeon(int ownerIndex, Axial position, Card card) : base(ownerIndex, position, card)
        {
        }
    }
}