public static class DialogueUtility
{
        public static string ORANGE = "#ff8629",
        RED = "#e63119",
        GREEN = "#41f27f",
        PINK = "#ed58aa",
        TEAL = "#2fd4ad";

        
        public static string C_()
        {
            return "[/color]";
        }

        public static string C_(string hexColor)
        {
            return $"[color={hexColor}]";
        }

}