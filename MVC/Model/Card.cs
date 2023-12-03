namespace Model
{
    public struct Card
    {
        public string NAME;

        public enum CardType { Base, Resource, Offense };
        public CardType TYPE;

        public int HP;
        public int MOVE;
        public int ATK;

        public static Card EMPTY = new Card("NULL", CardType.Base, -1);

        public Card(string name, CardType type, int hp)
        {
            NAME = name;
            TYPE = type;
            HP = hp;

            MOVE = 0;
            ATK = 0;
        }

        public Card(string name, int hp, int move, int atk)
        {
            NAME = name;
            TYPE = CardType.Offense;

            HP = hp;
            MOVE = move;
            ATK = atk;
        }

        public override string ToString()
        {
            return $"({NAME}, {TYPE}, HP:{HP}, MOVE:{MOVE}, ATK:{ATK})";
        }

        public static bool operator ==(Card a, Card b)
        {
            return a.NAME == b.NAME;
        }

        public static bool operator !=(Card a, Card b)
        {
            return a.NAME != b.NAME;
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