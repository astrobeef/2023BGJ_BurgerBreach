using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AxialCS;
using Godot;

namespace Model
{
    public partial class Unit_FamilyFries : Unit
    {
        private const int _HP_BUFF = 1;

        public Unit_FamilyFries(int ownerIndex, Axial position, Card card) : base(ownerIndex, position, card)
        {
        }

        protected override void OnTurnStart(int turnPlayerIndex, int turnCounter)
        {
            base.OnTurnStart(turnPlayerIndex, turnCounter);

            // Try to heal this to max health (cannot overheal)
            TryBuff(false, this.card.HP, 0);

            // Try to heal all neighbors (without prejudice) based on _HP_BUFF
            if(main.Instance.gameModel.ActiveBoard_FindAllNeighbors(this.pos, out int[] neighborActiveBoardIndexes))
            {
                for (int i = 0; i < neighborActiveBoardIndexes.Length; i++)
                {
                    int indexValue = neighborActiveBoardIndexes[i];
                    Unit neighbor = main.Instance.gameModel.ActiveBoard[indexValue];

                    neighbor.TryBuff(false, _HP_BUFF, 0);
                }
            }
        }
    }
}