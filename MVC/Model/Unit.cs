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

        public int hp;
        public int move;
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

            ResetTurnActions(ownerIndex, 0);

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
            if (newUnit == this)
            {
                main.Instance.SoundController?.Play(sound_controller.SFX_CARD_PLACE_NAME);
            }
        }

        protected virtual void OnUnitAttack(Unit attacker, Unit target)
        {
            if (attacker == this)
            {

            }
        }


        protected virtual void OnUnitMove(Axial oldPos, Unit movedUnit)
        {
            if (movedUnit == this)
            {
                main.Instance.SoundController?.Play(sound_controller.SFX_CARD_MOVE_NAME);
            }
        }

        protected virtual void OnUnitDamaged(Unit attacker, Unit target)
        {
            if (target == this)
            {
                main.Instance.SoundController?.Play(sound_controller.SFX_PLAYER_ATTACK_NAME);
            }
        }

        protected virtual void OnUnitBuffed(Unit target)
        {
            if(target == this)
            {
            }
        }

        protected virtual void OnUnitDeath(Unit deadUnit)
        {
            if (deadUnit == this)
            {
                UnsubscribeEvents();
            }
        }

        public virtual bool TryMove(bool isWillful, Unit unit, Axial newPos, out Unit occupant)
        {
            if (main.Instance.gameModel.IsLocationValidAndOpen(newPos, out occupant))
            {
                if (HasMovement(out int remainingMove) || !isWillful)
                {
                    GD.Print($"Attempting to move unit {unit.name} from {unit.pos} to {newPos}");

                    int displacement = Axial.Distance(unit.pos, newPos);

                    if(displacement > 1)
                        GD.PrintErr("Need to add logic to handle displacement > 1. Easy method would be to recursively check 'isLocationValid' one tile at a time along paths to target");

                    if (!isWillful || displacement <= remainingMove)
                    {
                        Axial oldPos = unit.pos;
                        unit.Move(isWillful, newPos);
                        return PostAction(main.Instance.gameModel.OnUnitMove, oldPos, unit);
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

        /// <summary>
        /// Move the unit instance on the board
        /// </summary>
        /// <param name="isWillful">Did this unit chose to move or was it forced to move?</param>
        /// <param name="newPos">New position to move towards</param>
        private void Move(bool isWillful, Axial newPos)
        {
            // GD.PrintErr("This is where TryMove should be called, not on the Model_Game. Event should still be called on Model.  It should be the model's job to identify which unit to perform the action on, but the unit's job to process the action.");

            int calculatedDisplacement = Axial.Distance(pos, newPos);
            if(isWillful)
                TurnActions.remainingMovement -= calculatedDisplacement;

            if (TurnActions.remainingMovement < 0)
                GD.PrintErr($"Unit {this} has its turn movement less than 0. Event should still be called on Model.  This should have been checked before this method.");

            pos = newPos;

            GD.Print($"Player {ownerIndex} moved unit {name} from {pos} to {newPos}. Calculated displacement: {calculatedDisplacement}. Movement remaining: {TurnActions.remainingMovement}");
        }



        public virtual bool TryAttack(Unit attacker, Unit target)
        {
            if (CanAttack(target))
            {
                PostAction(main.Instance.gameModel.OnUnitAttack, attacker, target);
                return Attack(target);
            }
            else return false;
        }

        protected virtual bool Attack(Unit target)
        {
            SetAttacked();
            TryDamage(this, this.atk, target);

            return true;
        }

        public virtual bool CanBeDamaged()
        {
            return true;
        }

        public virtual bool TryDamage(Unit attacker, int damage, Unit target)
        {
            if (target.CanBeDamaged())
            {
                PostAction(main.Instance.gameModel.OnUnitDamaged, attacker, target);
                target.Damage(damage);
                return true;
            }

            return false;
        }

        public virtual bool Damage(int amount)
        {
            if(hp < 0)
            {
                GD.PrintErr($"Unit {name} should have been removed already. Its being attacked, but its HP ({hp}) is already less than 0.");
            }

            hp -= amount;
            
            GD.Print($"Unit {name} has been damaged for {amount}. Remaining hp: {hp}");

            if(hp <= 0 && CanDie())
            {
                main.Instance.gameModel.ActiveBoard_RemoveUnit(this, out Unit dummyRemovedUnit);
            }

            return (hp > 0);
        }

        public bool CanDie()
        {
            return true;
        }

        public virtual bool TryBuff(bool canOverload, int hp_buff, int atk_buff)
        {
            if(CanBeBuffed())
            {
                bool isBuffed = false;

                if(canOverload || this.hp + hp_buff < this.card.HP)
                {
                    isBuffed = true;
                    this.hp += hp_buff;
                }
                    
                if(canOverload || this.atk + atk_buff < this.card.ATK)
                {
                    isBuffed = true;
                    this.atk += atk_buff;
                }
                if (isBuffed)
                    return PostAction(main.Instance.gameModel.OnUnitBuffed, this);
                else
                    return false;
            }

            return false;
        }

        protected virtual bool CanBeBuffed()
        {
            return true;
        }

        public bool CanAttack(Unit target)
        {
            if (Axial.Distance(this.pos, target.pos) <= ATK_RANGE)
            {
                return !TurnActions.hasAttacked;
            }
            else
                return false;
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