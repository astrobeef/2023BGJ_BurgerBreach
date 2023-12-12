using System;
using System.Runtime.InteropServices;
using AxialCS;
using Godot;
using static Model.ActionPoster;

namespace Model
{
    public partial class Unit_Base : Unit
    {
        public Unit_Base(int ownerIndex, Axial position, Card card) : base(ownerIndex, position, card)
        {
        }

        public override bool CanBeDamaged()
        {
            //If a friendly ClamChowder unit is on board
            if(main.Instance.gameModel.ActiveBoard_AllFriendlyUnits(ownerIndex, out Unit[] friendlyUnits))
            {
                foreach(Unit friendly in friendlyUnits)
                {
                    if(friendly as Unit_ClamChowder != null)
                    {
                        GD.Print($"{this.name} cannot be damaged because {friendly.name} is protecting it.");
                        return false;
                    }
                }
            }

            return base.CanBeDamaged();
        }

        public override bool TryMove(bool isWillful, Unit unit, Axial newPos, out Unit occupant)
        {
            occupant = null;
            return false;
        }

        public override bool Damage(int amount)
        {
            if (!base.Damage(amount))
            {
                GD.PrintErr($"Player {ownerIndex} has had their base destroyed! Need to fire an event to end the game.");
                PostAction(main.Instance.gameModel.OnBaseDestroyed, this);
                return true;
            }

            return true;
        }
    }
}