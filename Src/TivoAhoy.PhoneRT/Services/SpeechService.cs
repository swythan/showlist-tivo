using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using TivoAhoy.Common.Services;
using Windows.Phone.Speech.Recognition;
using Windows.Phone.Speech.Synthesis;
using Windows.Phone.Speech.VoiceCommands;

namespace TivoAhoy.PhoneRT.Services
{
    public class SpeechService : PropertyChangedBase, ISpeechService
    {
        public void EnsureInitVoiceCommandsOnBackgroundThread()
        {
            Task.Run(() => EnsureInitVoiceCommands());
        }

        private async Task EnsureInitVoiceCommands()
        {
            try
            {
                await VoiceCommandService.InstallCommandSetsFromFileAsync(new Uri("ms-appx:///VoiceCommands.xml"));
                //await UpdatePhraseListsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to register voice commands: {0}", ex);
            }
        }

        //private async Task UpdatePhraseListsAsync()
        //{
        //    foreach (VoiceCommandSet cs in VoiceCommandService.InstalledCommandSets.Values)
        //    {
        //        List<string> updatedListOfPhrases = GetPhrasesForUpdatedSiteToSearchPhraseList(cs.Language.ToLower());
        //        await cs.UpdatePhraseListAsync("siteToSearch", updatedListOfPhrases);
        //    }
        //}

        public async Task<string> RecognizeTextFromWebSearchGrammar(string exampleText)
        {
            string text = null;
            try
            {
                SpeechRecognizerUI sr = new SpeechRecognizerUI();
                sr.Recognizer.Grammars.AddGrammarFromPredefinedType("web", SpeechPredefinedGrammar.WebSearch);
                sr.Settings.ListenText = "Listening...";
                sr.Settings.ExampleText = exampleText;
                sr.Settings.ReadoutEnabled = false;
                sr.Settings.ShowConfirmation = false;


                SpeechRecognitionUIResult result = await sr.RecognizeWithUIAsync();
                if (result != null &&
                    result.ResultStatus == SpeechRecognitionUIStatus.Succeeded &&
                    result.RecognitionResult != null &&
                    result.RecognitionResult.TextConfidence != SpeechRecognitionConfidence.Rejected)
                {
                    text = result.RecognitionResult.Text;
                }
            }
            catch
            {
            }

            return text;
        }

        private string GetSpeechRecognitionLanguage()
        {
            return InstalledSpeechRecognizers.Default.Language.ToLower();
        }

        public async Task Speak(string text)
        {
            SpeechSynthesizer tts = new SpeechSynthesizer();
            await tts.SpeakTextAsync(text);
        }
    }
}
