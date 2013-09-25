//-----------------------------------------------------------------------
// <copyright file="ISpeechService.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

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
