using System;
using System.Runtime.InteropServices;
using AxialCS;
using Godot;

namespace Model
{
    public class CardInst
    {
        private static int lastId = 0;
        public int id {get; private set;}

        public AxialCS.Axial pos;
        public int ownerIndex;

        public Card card;
        public string name => card.NAME + $"({id})";
        public Card.CardType type => card.TYPE;

        public static CardInst EMPTY = new CardInst(-1, Axial.Empty, Card.EMPTY);


        public int hp;
        public int move;
        public int atk;
        public static readonly int ATK_RANGE = 1;

        private TurnActions TurnActions;

        public CardInst(int ownerIndex, Axial position, Card card)
        {
            this.ownerIndex = ownerIndex;
            this.pos = position;
            this.card = card;

            hp = card.HP;
            move = card.MOVE;
            atk = card.ATK;

            ResetTurnActions();
            
            id = System.Threading.Interlocked.Increment(ref lastId);
        }

        public void ResetTurnActions(){
            TurnActions = new TurnActions(move, (atk > 0));
        }

        public bool CanMove(out int remainingMove)
        {
            remainingMove = TurnActions.remainingMovement;
            return TurnActions.remainingMovement > 0;
        }

        /// <summary>
        /// Move the card instance on the board
        /// </summary>
        /// <param name="isWillful">Did this unit chose to move or was it forced to move?</param>
        /// <param name="newPos">New position to move towards</param>
        public void Move(bool isWillful, Axial newPos)
        {
            int calculatedDisplacement = Axial.Distance(pos, newPos);
            GD.Print($"Player {ownerIndex} moved card {card.NAME} from {pos} to {newPos}. Calculated displacement: {calculatedDisplacement}. Movement remaining: {TurnActions.remainingMovement}");

            if(isWillful)
                TurnActions.remainingMovement -= calculatedDisplacement;

            if (TurnActions.remainingMovement < 0)
                GD.PrintErr($"Card {this} has its turn movement less than 0. This should have been checked before this method.");

            pos = newPos;
        }

        public void Attack()
        {
            TurnActions.hasAttacked = true;
        }

        public bool Damage(int amount)
        {
            hp -= amount;
            
            GD.Print($"Card {card.NAME} has been damaged for {amount}. Remaining hp: {hp}");

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
        

        public static bool operator ==(CardInst a, CardInst b)
        {
            return a.id == b.id;
        }

        public static bool operator !=(CardInst a, CardInst b)
        {
            return a.id != b.id;
        }

        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object obj)
        {
            if (obj is CardInst obj_card)
            {
                return this == obj_card;
            }

            return false;
        }

        public override int GetHashCode()
        {
            // Use a prime number to combine hash codes in a way that reduces the chance of collisions
            const int prime = 31;
            int hash = 17;  // Another prime number
            hash = hash * prime + id;
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