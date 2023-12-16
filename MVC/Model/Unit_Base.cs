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
            occupant = Unit.EMPTY;
            return false;
        }

        public override bool Damage(int amount)
        {
            main.Instance.SoundController.Play(sound_controller.SFX_PLAYER_DRINK);

            if (!base.Damage(amount))
            {
                GD.Print($"Round has ended! Player {this.ownerIndex} lost the round! Triggering turn over.");
                main.Instance.gameModel.TriggerEndTurn = true;      //End the current turn to trigger the round ending
                
                return false;
            }

            return true;
        }
    }
}