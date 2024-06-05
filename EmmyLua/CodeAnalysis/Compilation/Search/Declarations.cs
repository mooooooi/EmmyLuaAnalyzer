﻿using EmmyLua.CodeAnalysis.Common;
using EmmyLua.CodeAnalysis.Compilation.Declaration;
using EmmyLua.CodeAnalysis.Syntax.Node;
using EmmyLua.CodeAnalysis.Syntax.Node.SyntaxNodes;
using EmmyLua.CodeAnalysis.Type;

namespace EmmyLua.CodeAnalysis.Compilation.Search;

public class Declarations(SearchContext context)
{
    private Dictionary<SyntaxElementId, IDeclaration?> DeclarationCaches { get; } = new();

    public IDeclaration? FindDeclaration(LuaSyntaxElement? element)
    {
        if (element is null)
        {
            return null;
        }

        if (context.Features.Cache &&  DeclarationCaches.TryGetValue(element.UniqueId, out var declaration))
        {
            return declaration;
        }

        declaration = InnerDeclaration(element);

        if (context.Features.Cache && declaration is not null)
        {
            DeclarationCaches[element.UniqueId] = declaration;
        }

        return declaration;
    }

    public IDeclaration? InnerDeclaration(LuaSyntaxElement? element)
    {
        switch (element)
        {
            case LuaNameExprSyntax nameExpr:
            {
                return FindNameDeclaration(nameExpr);
            }
            case LuaIndexExprSyntax indexExpr:
            {
                return FindIndexDeclaration(indexExpr);
            }
            case LuaTableFieldSyntax tableField:
            {
                return FindTableFieldDeclaration(tableField);
            }
            case LuaDocNameTypeSyntax docNameType:
            {
                return FindTypeDeclaration(docNameType.Name?.RepresentText);
            }
            case LuaDocGenericTypeSyntax docGenericType:
            {
                return FindTypeDeclaration(docGenericType.Name?.RepresentText);
            }
            case LuaDocTagClassSyntax docTagClass:
            {
                return FindTypeDeclaration(docTagClass.Name?.RepresentText);
            }
            case LuaDocTagInterfaceSyntax docTagInterface:
            {
                return FindTypeDeclaration(docTagInterface.Name?.RepresentText);
            }
            case LuaDocTagAliasSyntax docTagAlias:
            {
                return FindTypeDeclaration(docTagAlias.Name?.RepresentText);
            }
            case LuaDocTagEnumSyntax docTagEnum:
            {
                return FindTypeDeclaration(docTagEnum.Name?.RepresentText);
            }
            case LuaDocFieldSyntax docField:
            {
                return FindDocFieldDeclaration(docField);
            }
            case LuaDocTagNamedTypeSyntax docTagNamedType:
            {
                return FindTypeDeclaration(docTagNamedType.Name?.RepresentText);
            }
            case LuaParamDefSyntax or LuaLocalNameSyntax:
            {
                return context.Compilation.Db.QueryLocalDeclaration(element);
            }
        }

        return null;
    }

    private IDeclaration? FindNameDeclaration(LuaNameExprSyntax nameExpr)
    {
        if (nameExpr.Name is { Text: "self" })
        {
            var closures = nameExpr.Ancestors.OfType<LuaClosureExprSyntax>();
            foreach (var closure in closures)
            {
                var stat = closure.Parent;
                if (stat is LuaFuncStatSyntax { IndexExpr.PrefixExpr: { } expr })
                {
                    return FindDeclaration(expr);
                }
            }
        }

        var luaDeclaration = context.Compilation.Db.QueryLocalDeclaration(nameExpr);
        if (luaDeclaration is not null)
        {
            return luaDeclaration;
        }

        if (nameExpr.Name is { } name)
        {
            return context.Compilation.Db.QueryGlobals(name.RepresentText).FirstOrDefault();
        }

        return null;
    }

    private IDeclaration? FindTableFieldDeclaration(LuaTableFieldSyntax tableField)
    {
        if (tableField is { ParentTable: { } parentTable, Name: { } name })
        {
            var relatedType = context.Compilation.Db.QueryTypeFromId(parentTable.UniqueId) ??
                              new LuaTableLiteralType(parentTable);
            return context.FindMember(relatedType, name).FirstOrDefault();
        }

        return null;
    }

    private IDeclaration? FindIndexDeclaration(LuaIndexExprSyntax indexExpr)
    {
        if (indexExpr.PrefixExpr is { } prefixExpr)
        {
            var prefixType = context.Infer(prefixExpr);
            return context.FindMember(prefixType, indexExpr).FirstOrDefault();
        }

        return null;
    }

    private IDeclaration? FindTypeDeclaration(string? name)
    {
        if (name is not null)
        {
            return context.Compilation.Db.QueryNamedTypeDefinitions(name).FirstOrDefault();
        }

        return null;
    }

    private IDeclaration? FindDocFieldDeclaration(LuaDocFieldSyntax docField)
    {
        var parentType = context.Compilation.Db.QueryParentType(docField);
        if (parentType is not null && docField.Name is { } name)
        {
            return context.FindMember(parentType, name).FirstOrDefault();
        }

        return null;
    }

    public bool IsUpValue(LuaNameExprSyntax nameExpr, LuaDeclaration declaration)
    {
        if (nameExpr.Name is { Text: "self" })
        {
            return false;
        }

        var closure = nameExpr.Ancestors.OfType<LuaClosureExprSyntax>().FirstOrDefault();
        if (closure is not null)
        {
            return closure.Position > declaration.Position;
        }

        return false;
    }
}