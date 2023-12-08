namespace Model
{
    public struct Card
    {
        private static int lastId = 0;
        public int id {get; private set;}
        
        public string NAME;

        public enum CardType { Base, Resource, Offense };
        public CardType TYPE;

        public int HP;
        public int MOVE;
        public int ATK;

        public static Card EMPTY = new Card(false, "NULL", CardType.Base, -1);

        public Card(bool isUnique, string name, CardType type, int hp)
        {
            id = (isUnique)
             ? System.Threading.Interlocked.Increment(ref lastId)
              : -1;

            NAME = name;
            TYPE = type;
            HP = hp;

            MOVE = 0;
            ATK = 0;
        }

        public Card(bool isUnique, string name, int hp, int move, int atk)
        {
            id = (isUnique == true)
             ? System.Threading.Interlocked.Increment(ref lastId)
              : -1;

            NAME = name;
            TYPE = CardType.Offense;

            HP = hp;
            MOVE = move;
            ATK = atk;
        }

        public Card(bool isUnique, Card cardFromSet)
        {
            id = (isUnique == true)
             ? System.Threading.Interlocked.Increment(ref lastId)
              : -1;

            NAME = cardFromSet.NAME;
            TYPE = cardFromSet.TYPE;

            HP = cardFromSet.HP;
            MOVE = cardFromSet.MOVE;
            ATK = cardFromSet.ATK;
        }

        public override string ToString()
        {
            return $"(name:{NAME}, id:{id}, type:{TYPE},| HP:{HP}, MOVE:{MOVE}, ATK:{ATK})";
        }

        public static bool operator ==(Card a, Card b)
        {
            return a.NAME == b.NAME && a.id == b.id;
        }

        public static bool operator !=(Card a, Card b)
        {
            return !(a == b);
        }

        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object obj)
        {
            if (obj is Card obj_card)
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
            hash = hash * prime + NAME.GetHashCode();
            return hash;
        }
    }
}