using System;
using System.Collections.Generic;
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
        public int atk_range;

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
            atk_range = card.ATK_RANGE;

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

            GD.Print($"Unit [{this}] created");
        }

        private void UnsubscribeEvents()
        {
            Model_Game model = main.Instance.gameModel;
            
            model.OnTurnStart -= OnTurnStart;

            model.OnUnitAddedToBoard -= OnUnitAddedToBoard;
            model.OnUnitAttack -= OnUnitAttack;
            model.OnUnitDamaged -= OnUnitDamaged;
            model.OnUnitBuffed -= OnUnitBuffed;
            model.OnUnitDeath -= OnUnitDeath;
            model.OnUnitMove -= OnUnitMove;
        }

        protected virtual void OnTurnStart(int turnPlayerIndex, int turnCounter)
        {
            ResetTurnActions(turnPlayerIndex, turnCounter);
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
                // Do not move dead units
                if(unit.hp <= 0)
                {
                    GD.Print($"Unit {unit.name}@{unit.pos} cannot move because it is dead/dying");
                    return false;
                }

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
            else
            {
                return false;
            }
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

        /// <summary>
        /// Damage this unit
        /// </summary>
        /// <param name="amount">POSITIVE amount of damage</param>
        /// <returns></returns>
        public virtual bool Damage(int amount)
        {
            if(hp < 0)
            {
                GD.PrintErr($"Unit {name} should have been removed already. Its being attacked, but its HP ({hp}) is already less than 0.");
            }

            ChangeHP(-1 * amount);
            
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
                return Buff(canOverload, hp_buff, atk_buff);
            }

            return false;
        }

        protected virtual bool CanBeBuffed()
        {
            return true;
        }

        private bool Buff(bool canOverload, int hp_buff, int atk_buff)
        {
                bool isBuffed = false;

                if(canOverload || this.hp + hp_buff < this.card.HP)
                {
                    isBuffed = true;
                    ChangeHP(hp_buff);
                }
                    
                if(canOverload || this.atk + atk_buff < this.card.ATK)
                {
                    isBuffed = true;
                    ChangeATK(atk_buff);
                }
                if (isBuffed)
                    return PostAction(main.Instance.gameModel.OnUnitBuffed, this);
                else
                    return false;
        }

        public virtual bool CanAttack(Unit target)
        {
            if (Axial.Distance(this.pos, target.pos) <= this.atk_range)
            {
                GD.PrintErr($"Unit {this.name}@{this.pos} has attacked ? {TurnActions.hasAttacked}");
                return !TurnActions.hasAttacked;
            }
            else
            {
                GD.Print($"Unit {this.name}@{this.pos} cannot attack target {target.name}@{target.pos} because the target({Axial.Distance(this.pos, target.pos)} away) is out of range({this.atk_range}).");
                return false;
            }
        }

        public bool CanAttack()
        {
            return !TurnActions.hasAttacked;
        }

        public void SetAttacked()
        {
            TurnActions.hasAttacked = true;
        }

        public void ChangeHP(int additive)
        {
            GD.Print($"unit {this.name} is having ({additive}) added to its hp {this.hp}");
            this.hp += additive;
        }

        public void ChangeATK(int additive)
        {
            GD.Print($"unit {this.name} is having ({additive}) added to its atk {this.atk}");
            this.atk += additive;
        }



        /// <summary>
        /// Get all valid move positions for this unit at its current position 
        /// </summary>
        /// <param name="validMoves">All valid movement positions</param>
        /// <returns>True if at least one Axial is found</returns>
        /// <remarks>If you want to run this at a different Axial origin, use the override</remarks>
        public bool GetAllMovePositions(out Axial[] validMoves){
            return GetAllMovePositions(this.pos, out validMoves);
        }


        /// <summary>
        /// Get all valid move positions for this unit at its current position 
        /// </summary>
        /// <param name="origin">Where to originate this method call (useful for simulating this unit at a different position)</param>
        /// <param name="validMoves">All valid movement positions</param>
        /// <returns>True if at least one Axial is found</returns>
        /// <remarks>This override is meant to simulate the unit at a different origin</remarks>
        public bool GetAllMovePositions(Axial origin, out Axial[] validMoves)
        {
            if(GetAllMovePositions(origin, out Dictionary<Axial.Cardinal, Axial[]> validMovesDictionary))
            {
                List<Axial> validMoves_List = new List<Axial>();

                foreach(Axial[] moves in validMovesDictionary.Values)
                {
                    validMoves_List.AddRange(moves);
                }

                validMoves = validMoves_List.ToArray();
                return validMoves.Length > 0;
            }
            else
            {
                validMoves = null;
                return false;
            }
        }

        /// <summary>
        /// Get all valid move positions for this unit at its current position 
        /// </summary>
        /// <param name="origin">Where to originate this method call (useful for simulating this unit at a different position)</param>
        /// <param name="validMoves">All valid movement positions with keys in the direction of movement</param>
        /// <returns>True if at least one Axial is found</returns>
        /// <remarks>If you want to run this at a different Axial origin, use the override</remarks>
        public bool GetAllMovePositions(out Dictionary<Axial.Cardinal, Axial[]> validMovesDictionary)
        {
            return GetAllMovePositions(this.pos, out validMovesDictionary);
        }

        /// <summary>
        /// Get all valid move positions for this unit at its current position 
        /// </summary>
        /// <param name="origin">Where to originate this method call (useful for simulating this unit at a different position)</param>
        /// <param name="validMoves">All valid movement positions with keys in the direction of movement</param>
        /// <returns>True if at least one Axial is found</returns>
        /// <remarks>This override is meant to simulate the unit at a different origin</remarks>
        public bool GetAllMovePositions(Axial origin, out Dictionary<Axial.Cardinal, Axial[]> validMovesDictionary)
        {
            GD.Print($"!!! DISCLAIMER: This does not use path finding to see if an open tile within range can actually be moved towards (for instance, a blocked path would require more movement). We can get away with this because the only unit with more than 1 movement is the pigeon, which ignores units in its path");

            /*
            * The movement does not check for non-linear movements (for instance, starting going NE then moving E after that).
            * I could look at the grid generation code for how to handle this, but for now I'm leaving it as is
            */

            validMovesDictionary = new Dictionary<Axial.Cardinal, Axial[]>();

            if (this.HasMovement(out int remainingMove))
            {
                bool anyValidMoves = false;

                // For each cardinal direction
                for (int i = 0; i < Axial.CARDINAL_LENGTH; i++)
                {
                    List<Axial> validMove_List = new List<Axial>();

                    Axial.Cardinal card = (Axial.Cardinal)i;
                    Axial cardinalDirection = Axial.Direction((Axial.Cardinal)i);

                    // For each movement outwards from this unit up to its movement range
                    for (int j = 1; j <= remainingMove; j++)
                    {
                        Axial potentialDestination = origin + (j * cardinalDirection);

                        // If it is on the board and open
                        if (main.Instance.gameModel.IsLocationValidAndOpen(potentialDestination))
                        {
                            validMove_List.Add(potentialDestination);
                            anyValidMoves = true;
                        }
                    }
                    
                    validMovesDictionary.Add(card, validMove_List.ToArray());
                }

                return anyValidMoves;
            }
            else
            {
                return false;
            }
        }
        
        /// <summary>
        /// Get all valid targets (without prejudice)
        /// </summary>
        /// <param name="validTargets">All valid units</param>
        /// <returns>True if at least one unit is found</returns>
        /// <remarks>If you want to run this at a different Axial origin, use the override</remarks>
        public bool GetAllAttackTargets(out Unit[] validTargets){
            return GetAllAttackTargets(this.pos, out validTargets);
        }

        /// <summary>
        /// Get all valid targets (without prejudice)
        /// </summary>
        /// <param name="origin">Where to originate this method call (useful for simulating this unit at a different position)</param>
        /// <param name="validTargets">All valid units</param>
        /// <returns>True if at least one unit is found</returns>
        /// <remarks>This override is meant to simulate the unit at a different origin</remarks>
        public bool GetAllAttackTargets(Axial origin, out Unit[] validTargets)
        {
            if(this.CanAttack())
            {
                List<Unit> validTargets_List = new List<Unit>();

                // For each cardinal direction
                for (int i = 0; i < Axial.CARDINAL_LENGTH; i++)
                {
                    Axial cardinalDirection = Axial.Direction((Axial.Cardinal)i);

                    // For each movement outwards from this unit up to its movement range
                    for (int j = 1; j <= this.atk_range; j++)
                    {
                        Axial potentialTargetPos = origin + (j * cardinalDirection);

                        // If the Axial is occupied
                        if (main.Instance.gameModel.ActiveBoard_IsAxialOccupied(potentialTargetPos, out int activeBoardIndex))
                        {
                            Unit potentialTarget = main.Instance.gameModel.ActiveBoard[activeBoardIndex];
                            validTargets_List.Add(potentialTarget);
                            break;      //Do not continue further (assuming unit cannot penetrate targets)
                        }
                    }
                }

                validTargets = validTargets_List.ToArray();
                return validTargets.Length > 0;
            }
            else
            {
                validTargets = null;
                return false;
            }
        }

        /// <summary>
        /// Filter friendly targets in all attack targets
        /// </summary>
        /// <param name="validTargets"></param>
        /// <returns>Filtered friendly targets</returns>
        /// <remarks>For parameter, call <see cref="GetAllAttackTargets"/></remarks>
        public Unit[] GetAllAttackTargets_Friendly(Unit[] validTargets)
        {
            List<Unit> friendlyTargets = new List<Unit>();
            foreach(Unit unit in validTargets)
            {
                if(this.ownerIndex == unit.ownerIndex)
                    friendlyTargets.Add(unit);
            }

            return friendlyTargets.ToArray();
        }

        /// <summary>
        /// Filter enemy targets in all attack targets
        /// </summary>
        /// <param name="validTargets"></param>
        /// <returns>Filtered attack targets</returns>
        /// <remarks>For parameter, call <see cref="GetAllAttackTargets"/></remarks>
        public Unit[] GetAllAttackTargets_Enemy(Unit[] validTargets)
        {
            List<Unit> enemyTargets = new List<Unit>();
            foreach(Unit unit in validTargets)
            {
                if(this.ownerIndex != unit.ownerIndex)
                    enemyTargets.Add(unit);
            }

            return enemyTargets.ToArray();
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