﻿namespace EmmyLua.LanguageServer.Completion.CompleteProvider;

public interface ICompleteProviderBase
{
    public void AddCompletion(CompleteContext context);
}