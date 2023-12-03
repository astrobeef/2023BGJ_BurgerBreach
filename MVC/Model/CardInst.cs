namespace Model
{
    public class CardInst
    {
        public ushort id;

        public Card card;
        public string name => card.NAME;
        public Card.CardType type => card.TYPE;

        public static CardInst EMPTY => new CardInst(Card.EMPTY, 0);

        public int hp;
        public int move;
        public int atk;

        public CardInst(Card card, ushort id)
        {
            this.id = id;
            this.card = card;
            hp = card.HP;
            move = card.MOVE;
            atk = card.ATK;
        }
    }
}