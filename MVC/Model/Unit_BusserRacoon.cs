using System;
using System.Runtime.InteropServices;
using AxialCS;
using Godot;

namespace Model
{
    public partial class Unit_BusserRacoon : Unit
    {
        public Unit_BusserRacoon(int ownerIndex, Axial position, Card card) : base(ownerIndex, position, card)
        {
        }
    }
}