using Godot;

namespace Model
{
    public struct Card
    {
        private static uint lastId = 0;
        public uint id {get; private set;}

        public const string BASE_NAME = "Base",
            RESOURCE_TEST_NAME = "Resource Test",
            OFFENSE_TEST_NAME = "Offense Test",
            BIG_MOE_NAME = "Big Moe",
            LINE_SQUIRREL_NAME = "Line Squirrel",
            EXPO_PIGEON_NAME = "Expo Pigeon",
            BUSSER_RACOON_NAME = "Busser Racoon",
            CLAM_CHOWDER_NAME = "Moe's Clam Chowder",
            BURGER_NAME = "Big Moe's Burger",
            MOE_FAMILY_FRIES_NAME = "Moe's Family Fries",
            THE_SCRAPS_NAME = "The Scraps";
        
        public string NAME;

        public enum CardType { Base, Resource, Offense };
        public CardType TYPE;

        public int HP;
        public int MOVE;
        public int ATK;
        public int ATK_RANGE;

        public static Card EMPTY = new Card(false, "NULL", CardType.Base, -1);

        public Card(bool isUnique, string name, CardType type, int hp)
        {
            id = (isUnique)
             ? System.Threading.Interlocked.Increment(ref lastId)
              : 0;

            NAME = name;
            TYPE = type;
            HP = hp;

            MOVE = 0;
            ATK = 0;
            ATK_RANGE = 1;
        }

        public Card(bool isUnique, string name, int hp, int move, int atk, int atk_range)
        {
            id = (isUnique == true)
             ? System.Threading.Interlocked.Increment(ref lastId)
              : 0;

            NAME = name;
            TYPE = CardType.Offense;

            HP = hp;
            MOVE = move;
            ATK = atk;
            ATK_RANGE = atk_range;
        }

        public Card(bool isUnique, Card cardFromSet)
        {
            id = (isUnique == true)
             ? System.Threading.Interlocked.Increment(ref lastId)
              : 0;

            NAME = cardFromSet.NAME;
            TYPE = cardFromSet.TYPE;

            HP = cardFromSet.HP;
            MOVE = cardFromSet.MOVE;
            ATK = cardFromSet.ATK;
            ATK_RANGE = cardFromSet.ATK_RANGE;
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