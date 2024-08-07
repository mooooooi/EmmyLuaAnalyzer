﻿using EmmyLua.CodeAnalysis.Compilation.Search;
using EmmyLua.CodeAnalysis.Compilation.Type;
using EmmyLua.LanguageServer.Framework.Protocol.Message.DocumentSymbol;
using EmmyLua.LanguageServer.Server;
using EmmyLua.LanguageServer.Util;

namespace EmmyLua.LanguageServer.WorkspaceSymbol;

public class WorkspaceSymbolBuilder
{
    public List<Framework.Protocol.Message.WorkspaceSymbol.WorkspaceSymbol> Build(string query,
        ServerContext context, CancellationToken cancellationToken)
    {
        var result = new List<Framework.Protocol.Message.WorkspaceSymbol.WorkspaceSymbol>();
        try
        {
            var luaWorkspace = context.LuaWorkspace;
            var searchContext = new SearchContext(luaWorkspace.Compilation, new SearchContextFeatures());
            var globals = context.LuaWorkspace.Compilation.Db.QueryAllGlobal();
            foreach (var global in globals)
            {
                if (global.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var location = global.GetLocation(searchContext)?.ToLspLocation();
                    if (location is not null)
                    {
                        result.Add(new Framework.Protocol.Message.WorkspaceSymbol.WorkspaceSymbol()
                        {
                            Name = global.Name,
                            Kind = ToSymbolKind(global.Type),
                            Location = location
                        });
                    }
                }
            }

            var members = context.LuaWorkspace.Compilation.Db.QueryAllMembers();
            foreach (var member in members)
            {
                if (member.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var document = luaWorkspace.GetDocument(member.Info.Ptr.DocumentId);
                    if (document is not null && member.Info.Ptr.ToNode(document) is { } node)
                    {
                        result.Add(new Framework.Protocol.Message.WorkspaceSymbol.WorkspaceSymbol()
                        {
                            Name = member.Name,
                            Kind = ToSymbolKind(member.Info.DeclarationType),
                            Location = node.Range.ToLspLocation(document)
                        });
                    }
                }
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            return result;
        }
    }

    private static SymbolKind ToSymbolKind(LuaType? type)
    {
        return type switch
        {
            LuaNamedType => SymbolKind.Variable,
            LuaMethodType => SymbolKind.Method,
            _ => SymbolKind.Variable
        };
    }
}