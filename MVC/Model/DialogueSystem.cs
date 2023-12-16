using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace Model
{
    public class DialogueSystem
    {
        public string CurrentMessage => _currentMessage;
        private string _currentMessage;

        private const int LETTER_DELAY = 20; // Delay for letters and numbers
        private const int PAUSE_DELAY = 100; // Delay for commas
        private const int END_SENTENCE_DELAY = 200; // Delay for period, exclamation, question mark
        private const int OTHER_CHAR_DELAY = 10; // Delay for other characters

        private enum CharType { letter, comma, end, other };

        public RichTextLabel DEBUG_richTextLabel;


        private string[] _Queue;

        public DialogueSystem()
        {
            DEBUG_richTextLabel = main.CurrentScene.FindChild("DEBUG_DialogueRichText") as RichTextLabel;
            if (DEBUG_richTextLabel == null) throw new Exception("Could not find richTextLabel named \"DEBUG_DialogueRichText\"");
        }

        bool isWriting = false, skipPendingMessages = false;

        private CancellationTokenSource cancellationTokenSource;

        private async void DisplayMessage(string message)
        {
            CancellationToken cancellationToken = cancellationDictionary[message];

            if (!main.IsMainThread())
            {
                GD.PrintErr("Cannot execute this method from an offload thread");
                cancellationDictionary.Remove(message);
                isWriting = false;
                return;
            }

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

            Task display = new Task(async () =>
            {
                StringBuilder sb = new StringBuilder();
                bool insideBBCodeTag = false;

                // Build the message letter by letter
                foreach (char c in message)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationDictionary.Remove(message);
                        isWriting = false;
                        return;
                    }

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
                                // System.Threading.Thread.Sleep(LETTER_DELAY);
                                await Task.Delay(LETTER_DELAY);
                                break;
                            case CharType.comma:
                                // System.Threading.Thread.Sleep(PAUSE_DELAY);
                                await Task.Delay(PAUSE_DELAY);
                                break;
                            case CharType.end:
                                // System.Threading.Thread.Sleep(END_SENTENCE_DELAY);
                                await Task.Delay(END_SENTENCE_DELAY);
                                break;
                            case CharType.other:
                                // System.Threading.Thread.Sleep(OTHER_CHAR_DELAY);
                                await Task.Delay(OTHER_CHAR_DELAY);
                                break;
                        }
                    }
                }

            });

            display.RunSynchronously();

            await Task.WhenAll(new Task[] { display });

            cancellationDictionary.Remove(message);
            isWriting = false;
        }

        Dictionary<string, CancellationToken> cancellationDictionary = new Dictionary<string, CancellationToken>();

        public async void QueueMessage(bool interrupt, string message)
        {
            // Always interrupting because I can't figure out why messages are conflicting when they don't interrupt
            interrupt = true;

            if (cancellationDictionary.ContainsKey(message))
                return;

            if (interrupt)
            {
                if (cancellationTokenSource != null)
                {
                    // Cancel the ongoing task and skip pending messages
                    cancellationTokenSource.Cancel();
                    skipPendingMessages = true;
                }
            }

            // Wait for the previous message to finish or be skipped
            while (!interrupt && isWriting && !skipPendingMessages)
            {
                await Task.Delay(50);
            }

            if (skipPendingMessages && !interrupt)
            {
                // Skip this message as an interrupt has occurred
                return;
            }

            isWriting = true;
            skipPendingMessages = false;

            cancellationTokenSource = new CancellationTokenSource();
            cancellationDictionary.Add(message, cancellationTokenSource.Token);

            if (main.IsMainThread())
            {
                DisplayMessage(message);
            }
            else
            {
                ActionPoster.PostAction(DisplayMessage, message);
            }
        }
    }
}