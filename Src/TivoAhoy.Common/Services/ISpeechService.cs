using System;
using System.Threading.Tasks;
namespace TivoAhoy.Common.Services
{
    public interface ISpeechService
    {
        void EnsureInitVoiceCommandsOnBackgroundThread();

        Task<string> RecognizeTextFromWebSearchGrammar(string exampleText);
        
        Task Speak(string text);
    }
}
