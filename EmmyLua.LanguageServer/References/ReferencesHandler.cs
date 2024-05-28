﻿using EmmyLua.LanguageServer.Server;
using EmmyLua.LanguageServer.Util;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace EmmyLua.LanguageServer.References;

// ReSharper disable once ClassNeverInstantiated.Global
public class ReferencesHandler(ServerContext context) : ReferencesHandlerBase
{
    protected override ReferenceRegistrationOptions CreateRegistrationOptions(ReferenceCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new ReferenceRegistrationOptions()
        {
        };
    }

    public override Task<LocationContainer?> Handle(ReferenceParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToUri().AbsoluteUri;
        LocationContainer? locationContainer = null;
        context.ReadyRead(() =>
        {
            var semanticModel = context.GetSemanticModel(uri);
            if (semanticModel is not null)
            {
                var document = semanticModel.Document;
                var pos = request.Position;
                var node = document.SyntaxTree.SyntaxRoot.NameNodeAt(pos.Line, pos.Character);
                if (node is not null)
                {
                    var references = semanticModel.FindReferences(node);
                    locationContainer = LocationContainer.From(
                        references.Select(it => it.Location.ToLspLocation())
                    );
                }
            }
        });

        return Task.FromResult<LocationContainer?>(locationContainer);
    }
}