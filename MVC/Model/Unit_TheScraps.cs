using System;
using System.Runtime.InteropServices;
using AxialCS;
using Godot;

namespace Model
{
    public partial class Unit_TheScraps : Unit
    {
        public Unit_TheScraps(int ownerIndex, Axial position, Card card) : base(ownerIndex, position, card)
        {
        }
    }
}