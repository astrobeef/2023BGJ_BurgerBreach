using System;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace Model
{
    public class DialogueSystem
    {
        public string CurrentMessage => _currentMessage;
        private string _currentMessage;

        private const int LETTER_DELAY = 50; // Delay for letters and numbers
        private const int PAUSE_DELAY = 250; // Delay for commas
        private const int END_SENTENCE_DELAY = 500; // Delay for period, exclamation, question mark
        private const int OTHER_CHAR_DELAY = 30; // Delay for other characters

        private enum CharType {letter, comma, end, other};

        private static string ORANGE = "#ff8629",
        RED = "#e63119",
        GREEN = "#41f27f",
        PINK = "#ed58aa",
        TEAL = "#2fd4ad";

        public RichTextLabel DEBUG_richTextLabel;


        private string[] _Queue;

        public DialogueSystem()
        {
            DEBUG_richTextLabel = main.CurrentScene.FindChild("DEBUG_DialogueRichText") as RichTextLabel;
            if(DEBUG_richTextLabel == null) throw new Exception("Could not find richTextLabel named \"DEBUG_DialogueRichText\"");
            
            DisplayMessage("So you probably thought: \"Huh, two weird people come in talking about mode, I guess that's why he calls its the mode of '87'\". But there's more... We'd agree wednesday is the third day of the week, right? Well, on the third Wednesday of March in '87, these triplets come strolling in at 3:33pm hootin and howlerin about how they just won the lottery. Next thing you know this drifter, same guy I was talking about earlier, stops eating his burger after the third bite and pulls out a triple barreled sawed off shotgun and he says to them: \"Easy mode or hard mode, doesn't matter to me. Either way I'm getting that lotto.\"");
        }
        
        private static string C_()
        {
            return "[/color]";
        }

        private string C_(string hexColor)
        {
            return $"[color={hexColor}]";
        }

        public void DisplayMessage(string message)
        {
            int charCount = 0, charCountMax = 3;

            IProgress<(string, CharType)> ProgressReporter = new Progress<(string, CharType)>((tuple) =>
            {
                var (message, charType) = tuple;
                charCount++;

                // GD.Print(message);
                DEBUG_richTextLabel.Text = message;

                if (charType == CharType.letter)
                {
                    switch (charCount % charCountMax)
                    {
                        case 0:
                            main.Instance.SoundController.PlaySFX(sound_controller.SFX_DIALOGUE_M);
                            break;
                        case 1:
                            main.Instance.SoundController.PlaySFX(sound_controller.SFX_DIALOGUE_O);
                            break;
                        case 2:
                            main.Instance.SoundController.PlaySFX(sound_controller.SFX_DIALOGUE_E);
                            break;
                    }
                }
            });

            Task display = new Task(() =>
            {
                StringBuilder sb = new StringBuilder();
            bool insideBBCodeTag = false;

            // Build the message letter by letter
            foreach (char c in message)
            {
                CharType charType;
                // If BBcode is starting
                if (c == '[')
                {
                    insideBBCodeTag = true;
                }

                sb.Append(c);

                // If BBcode is ending
                if (c == ']' && insideBBCodeTag)
                {
                    insideBBCodeTag = false;
                }

                    // Neglect to report BBcode, only report when we're outside of BBcode
                    if (!insideBBCodeTag)
                    {
                        if (c == ',')
                        {
                            charType = CharType.comma;

                        }
                        else if (c == '.' || c == '!' || c == '?')
                        {
                            charType = CharType.end;
                        }
                        else if (!char.IsLetterOrDigit(c))
                        {
                            charType = CharType.other;
                        }
                        else
                        {
                            charType = CharType.letter;
                        }

                        ProgressReporter.Report((sb.ToString(), charType));

                        //Do this after so that the delay comes after the char appears
                        switch (charType)
                        {
                            case CharType.letter:
                                System.Threading.Thread.Sleep(LETTER_DELAY);
                                break;
                            case CharType.comma:
                                System.Threading.Thread.Sleep(PAUSE_DELAY);
                                break;
                            case CharType.end:
                                System.Threading.Thread.Sleep(END_SENTENCE_DELAY);
                                break;
                            case CharType.other:
                                System.Threading.Thread.Sleep(OTHER_CHAR_DELAY);
                                break;
                        }
                    }
                }

            });



            // Ensure this is run off main thread
            if (main.IsMainThread())
            {
                display.Start();
            }
            else
            {
                display.RunSynchronously();
            }
        }

        public void QueueMessage(string message)
        {

        }
    }
}