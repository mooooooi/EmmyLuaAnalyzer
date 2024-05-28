﻿using System.Text;
using EmmyLua.CodeAnalysis.Document.Version;
using EmmyLua.CodeAnalysis.Kind;
using EmmyLua.CodeAnalysis.Syntax.Tree;
using EmmyLua.CodeAnalysis.Syntax.Tree.Green;

namespace EmmyLua.CodeAnalysis.Syntax.Node.SyntaxNodes;

public class LuaSourceSyntax(GreenNode greenNode, LuaSyntaxTree tree) : LuaSyntaxNode(greenNode, tree, null, 0)
{
    public LuaBlockSyntax? Block => FirstChild<LuaBlockSyntax>();
}

public class LuaBlockSyntax(GreenNode greenNode, LuaSyntaxTree tree, LuaSyntaxElement? parent, int startOffset)
    : LuaSyntaxNode(greenNode, tree, parent, startOffset)
{
    public IEnumerable<LuaStatSyntax> StatList => ChildNodes<LuaStatSyntax>();

    public IEnumerable<LuaCommentSyntax> Comments =>
        Tree.BinderData?.GetComments(this) ?? [];
}

public class LuaParamDefSyntax(GreenNode greenNode, LuaSyntaxTree tree, LuaSyntaxElement? parent, int startOffset)
    : LuaSyntaxNode(greenNode, tree, parent, startOffset)
{
    public LuaNameToken? Name => FirstChild<LuaNameToken>();

    public bool IsVarArgs => FirstChild<LuaDotsToken>() != null;
}

public class LuaParamListSyntax(GreenNode greenNode, LuaSyntaxTree tree, LuaSyntaxElement? parent, int startOffset)
    : LuaSyntaxNode(greenNode, tree, parent, startOffset)
{
    public IEnumerable<LuaParamDefSyntax> Params => ChildNodes<LuaParamDefSyntax>();

    public bool HasVarArgs => Params.LastOrDefault()?.IsVarArgs == true;
}

public class LuaAttributeSyntax(GreenNode greenNode, LuaSyntaxTree tree, LuaSyntaxElement? parent, int startOffset)
    : LuaSyntaxNode(greenNode, tree, parent, startOffset)
{
    public LuaNameToken? Name => FirstChild<LuaNameToken>();

    public bool IsConst
    {
        get
        {
            if (Name == null)
            {
                return false;
            }

            return Name.Text is "const";
        }
    }

    public bool IsClose
    {
        get
        {
            if (Name == null)
            {
                return false;
            }

            return Name.Text is "close";
        }
    }
}

public class LuaLocalNameSyntax(GreenNode greenNode, LuaSyntaxTree tree, LuaSyntaxElement? parent, int startOffset)
    : LuaSyntaxNode(greenNode, tree, parent, startOffset)
{
    public LuaAttributeSyntax? Attribute => FirstChild<LuaAttributeSyntax>();

    public LuaNameToken? Name => FirstChild<LuaNameToken>();
}

public class LuaCallArgListSyntax(GreenNode greenNode, LuaSyntaxTree tree, LuaSyntaxElement? parent, int startOffset)
    : LuaSyntaxNode(greenNode, tree, parent, startOffset)
{
    public IEnumerable<LuaExprSyntax> ArgList => ChildNodes<LuaExprSyntax>();

    public bool IsSingleArgCall => FirstChildToken(LuaTokenKind.TkLeftParen) != null;

    public LuaExprSyntax? SingleArg => FirstChild<LuaExprSyntax>();

    public LuaSyntaxToken? LeftParen => FirstChildToken(LuaTokenKind.TkLeftParen);

    public LuaSyntaxToken? RightParen => FirstChildToken(LuaTokenKind.TkRightParen);
}

public class LuaDescriptionSyntax(GreenNode greenNode, LuaSyntaxTree tree, LuaSyntaxElement? parent, int startOffset)
    : LuaSyntaxNode(greenNode, tree, parent, startOffset)
{
    public IEnumerable<LuaSyntaxToken> Details => ChildTokens(LuaTokenKind.TkDocDetail);

    public string CommentText
    {
        get
        {
            var sb = new StringBuilder();
            foreach (var token in ChildrenWithTokens)
            {
                if (token is LuaSyntaxToken { Kind: LuaTokenKind.TkDocDetail , RepresentText: {} text})
                {
                    if (text.StartsWith('@') || text.StartsWith('#'))
                    {
                        sb.Append(text[1..]);
                    }
                    else
                    {
                        sb.Append(text);
                    }
                }
                else if(token is LuaSyntaxToken { Kind: LuaTokenKind.TkDocContinue, Range.Length: {} length })
                {
                    if (length > 3)
                    {
                        sb.Append(' ', length - 3);
                    }
                }
                else if (token is LuaSyntaxToken { Kind: LuaTokenKind.TkEndOfLine })
                {
                    sb.Append('\n');
                }
            }

            return sb.ToString();
        }
    }
}

public class LuaDocFieldSyntax(GreenNode greenNode, LuaSyntaxTree tree, LuaSyntaxElement? parent, int startOffset)
    : LuaSyntaxNode(greenNode, tree, parent, startOffset)
{
    public VisibilityKind Visibility
    {
        get
        {
            var tk = FirstChildToken(LuaTokenKind.TkTagVisibility);
            return tk == null ? VisibilityKind.Public : VisibilityKindHelper.ToVisibilityKind(tk.Text);
        }
    }

    public bool IsNameField => FirstChildToken(LuaTokenKind.TkName) != null;

    public bool IsStringField => FirstChildToken(LuaTokenKind.TkString) != null;

    public bool IsIntegerField => FirstChildToken(LuaTokenKind.TkInt) != null;

    public bool IsTypeField =>
        ChildNodesBeforeToken<LuaDocTypeSyntax>(LuaTokenKind.TkRightBracket).FirstOrDefault() != null;

    public LuaNameToken? NameField => FirstChild<LuaNameToken>();

    public LuaStringToken? StringField => FirstChild<LuaStringToken>();

    public LuaIntegerToken? IntegerField => FirstChild<LuaIntegerToken>();

    public LuaDocTypeSyntax? TypeField =>
        ChildNodesBeforeToken<LuaDocTypeSyntax>(LuaTokenKind.TkRightBracket).FirstOrDefault();

    public bool Nullable => FirstChildToken(LuaTokenKind.TkNullable) != null;

    public LuaSyntaxElement? FieldElement
    {
        get
        {
            if (IsNameField)
            {
                return NameField!;
            }

            if (IsStringField)
            {
                return StringField!;
            }

            if (IsIntegerField)
            {
                return IntegerField!;
            }

            return null;
        }
    }

    public string? Name
    {
        get
        {
            if (IsNameField)
            {
                return NameField!.RepresentText;
            }

            if (IsStringField)
            {
                return StringField!.Value;
            }

            if (IsIntegerField)
            {
                return $"[{IntegerField!.Value}]";
            }

            return null;
        }
    }

    public LuaDocTypeSyntax? Type => IsTypeField
        ? ChildNodes<LuaDocTypeSyntax>().LastOrDefault()
        : FirstChild<LuaDocTypeSyntax>();

    public LuaDescriptionSyntax? Description => FirstChild<LuaDescriptionSyntax>();
}

public class LuaDocBodySyntax(GreenNode greenNode, LuaSyntaxTree tree, LuaSyntaxElement? parent, int startOffset)
    : LuaSyntaxNode(greenNode, tree, parent, startOffset)
{
    public IEnumerable<LuaDocFieldSyntax> FieldList => ChildNodes<LuaDocFieldSyntax>();
}

public class LuaDocVersionSyntax(GreenNode greenNode, LuaSyntaxTree tree, LuaSyntaxElement? parent, int startOffset)
    : LuaSyntaxNode(greenNode, tree, parent, startOffset)
{
    public RequiredVersionAction Action
    {
        get
        {
            var tk = FirstChildToken();
            return tk?.Kind switch
            {
                LuaTokenKind.TkGt => RequiredVersionAction.Greater,
                LuaTokenKind.TkGe => RequiredVersionAction.GreaterOrEqual,
                LuaTokenKind.TkLt => RequiredVersionAction.Less,
                LuaTokenKind.TkLe => RequiredVersionAction.LessOrEqual,
                _ => RequiredVersionAction.Equal
            };
        }
    }

    public LuaSyntaxToken? Version => FirstChildToken(LuaTokenKind.TkName);

    public LuaVersionNumberToken? VersionNumber => FirstChild<LuaVersionNumberToken>();
}
