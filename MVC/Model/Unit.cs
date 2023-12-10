using System;
using System.Runtime.InteropServices;
using AxialCS;
using Godot;

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
            this.ownerIndex = ownerIndex;
            this.pos = position;
            this.card = card;

            hp = card.HP;
            move = card.MOVE;
            atk = card.ATK;

            main.Instance.gameModel.OnTurnStart += ResetTurnActions;

            id = System.Threading.Interlocked.Increment(ref lastId);
        }

        public void ResetTurnActions(int turnPlayerIndex, int turnCounter)
        {
            if (turnPlayerIndex == ownerIndex)
                TurnActions = new TurnActions(move, (atk > 0));
        }

        public bool CanMove(out int remainingMove)
        {
            remainingMove = TurnActions.remainingMovement;
            return TurnActions.remainingMovement > 0;
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