using System;
using System.Runtime.InteropServices;
using AxialCS;
using Godot;

namespace Model
{
    public partial class Unit_ClamChowder : Unit
    {
        public Unit_ClamChowder(int ownerIndex, Axial position, Card card) : base(ownerIndex, position, card)
        {
        }
    }
}