using System;
using System.Runtime.InteropServices;
using AxialCS;
using Godot;
using static Model.ActionPoster;

namespace Model
{
    public partial class Unit : Resource
    {
        private static uint lastId = 0;
        public uint id {get; private set;}

        public AxialCS.Axial pos;
        public int ownerIndex;

        public Card card;
        public string name => card.NAME + $"({id})";
        public Card.CardType type => card.TYPE;

        public static Unit EMPTY = new Unit(-1, Axial.Empty, Card.EMPTY);

        [Export]
        public int hp;
        [Export]
        public int move;
        [Export]
        public int atk;
        public static readonly int ATK_RANGE = 1;

        private TurnActions TurnActions;

        public Unit(int ownerIndex, Axial position, Card card)
        {
            if (card == Card.EMPTY)
                return;

            this.ownerIndex = ownerIndex;
            this.pos = position;
            this.card = card;

            hp = card.HP;
            move = card.MOVE;
            atk = card.ATK;

            Model_Game model = main.Instance.gameModel;

            model.OnTurnStart += ResetTurnActions;

            model.OnUnitAddedToBoard += OnUnitAddedToBoard;
            model.OnUnitAttack += OnUnitAttack;
            model.OnUnitDamaged += OnUnitDamaged;
            model.OnUnitBuffed += OnUnitBuffed;
            model.OnUnitDeath += OnUnitDeath;
            model.OnUnitMove += OnUnitMove;

            id = System.Threading.Interlocked.Increment(ref lastId);
        }

        private void UnsubscribeEvents()
        {
            Model_Game model = main.Instance.gameModel;
            
            model.OnTurnStart -= ResetTurnActions;

            model.OnUnitAddedToBoard -= OnUnitAddedToBoard;
            model.OnUnitAttack -= OnUnitAttack;
            model.OnUnitDamaged -= OnUnitDamaged;
            model.OnUnitBuffed -= OnUnitBuffed;
            model.OnUnitDeath -= OnUnitDeath;
            model.OnUnitMove -= OnUnitMove;
        }

        protected void ResetTurnActions(int turnPlayerIndex, int turnCounter)
        {
            if (turnPlayerIndex == ownerIndex)
                TurnActions = new TurnActions(move, (atk > 0));
        }

        protected virtual void OnUnitAddedToBoard(Unit newUnit)
        {
            main.Instance.SoundController?.Play(sound_controller.SFX_CARD_PLACE_NAME);
        }

        protected virtual void OnUnitAttack(Unit attacker, Unit target)
        {
        }

        protected virtual void OnUnitDamaged(Unit target)
        {
            main.Instance.SoundController?.Play(sound_controller.SFX_PLAYER_ATTACK_NAME);
        }

        protected virtual void OnUnitBuffed(Unit target)
        {
        }

        protected virtual void OnUnitDeath(Unit deadUnit)
        {
            UnsubscribeEvents();
        }

        public virtual bool TryMove(bool isWillful, Unit unit, Axial newPos, out Unit occupant)
        {
            if (main.Instance.gameModel.IsLocationValidAndOpen(newPos, out occupant))
            {
                if (HasMovement(out int remainingMove))
                {
                    GD.Print($"Attempting to move unit {unit.name} from {unit.pos} to {newPos}");

                    int displacement = Axial.Distance(unit.pos, newPos);

                    if(displacement > 1)
                        GD.PrintErr("Need to add logic to handle displacement > 1. Easy method would be to recursively check 'isLocationValid' one tile at a time along paths to target");

                    if (!isWillful || displacement <= remainingMove)
                    {
                        unit.Move(isWillful, newPos);
                        return true;
                    }
                    else
                    {
                        GD.Print($"Unit {unit.name} cannot move because the displacement ({displacement}) is greater than the remaining movement ({remainingMove}) AND the movement is willful ({isWillful})");
                        return false;
                    }
                }
                else
                {
                    GD.Print($"Unit {unit.name} cannot move, according to unit data.");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public virtual bool HasMovement(out int remainingMove)
        {
            remainingMove = TurnActions.remainingMovement;
            return TurnActions.remainingMovement > 0;
        }

        protected virtual void OnUnitMove(Axial oldPos, Unit movedUnit)
        {
            main.Instance.SoundController?.Play(sound_controller.SFX_CARD_MOVE_NAME);
        }


        /// <summary>
        /// Move the unit instance on the board
        /// </summary>
        /// <param name="isWillful">Did this unit chose to move or was it forced to move?</param>
        /// <param name="newPos">New position to move towards</param>
        public void Move(bool isWillful, Axial newPos)
        {
            GD.PrintErr("This is where TryMove should be called, not on the Model_Game. Event should still be called on Model.  It should be the model's job to identify which unit to perform the action on, but the unit's job to process the action.");

            int calculatedDisplacement = Axial.Distance(pos, newPos);
            if(isWillful)
                TurnActions.remainingMovement -= calculatedDisplacement;

            if (TurnActions.remainingMovement < 0)
                GD.PrintErr($"Unit {this} has its turn movement less than 0. Event should still be called on Model.  This should have been checked before this method.");

            pos = newPos;

            GD.Print($"Player {ownerIndex} moved unit {name} from {pos} to {newPos}. Calculated displacement: {calculatedDisplacement}. Movement remaining: {TurnActions.remainingMovement}");
        }

        public void Attack()
        {
            GD.PrintErr("This is where TryAttack should be called, not on the Model_Game. It should be the model's job to identify which unit to perform the action on, but the unit's job to process the action.");

            TurnActions.hasAttacked = true;
        }

        public bool Damage(int amount)
        {
            GD.PrintErr("This is where TryDamage should be called, not on the Model_Game. Event should still be called on Model. It should be the model's job to identify which unit to perform the action on, but the unit's job to process the action.");

            if(hp < 0)
            {
                GD.PrintErr($"Unit {name} should have been removed already. Its being attacked, but its HP ({hp}) is already less than 0.");
            }

            hp -= amount;
            
            GD.Print($"Unit {name} has been damaged for {amount}. Remaining hp: {hp}");

            return (hp > 0);
        }

        public bool CanAttack()
        {
            return !TurnActions.hasAttacked;
        }

        public void SetAttacked()
        {
            TurnActions.hasAttacked = true;
        }

        public override string ToString()
        {
            return $"(owner:{ownerIndex}, pos:{pos}, hp:{hp}, || id:{id}, move:{move}, atk:{atk}, remainingMove:{TurnActions.remainingMovement}, hasAttacked:{TurnActions.hasAttacked} card:{card})";
        }

        public static bool operator ==(Unit a, Unit b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
                return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
                return false;

            return a.id == b.id;
        }

        public static bool operator !=(Unit a, Unit b)
        {
            return !(a == b);
        }

        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object obj)
        {
            if (obj is Unit obj_unit)
            {
                return this == obj_unit;
            }

            return false;
        }

        public override int GetHashCode()
        {
            // Use a prime number to combine hash codes in a way that reduces the chance of collisions
            const int prime = 31;
            int hash = 17;  // Another prime number
            hash = hash * prime + (int)id;
            return hash;
        }
    }

    internal struct TurnActions
    {
        internal int remainingMovement;
        internal bool hasAttacked;

        internal TurnActions(int move, bool canAttack)
        {
            remainingMovement = move;
            hasAttacked = !canAttack;
        }
    }
}