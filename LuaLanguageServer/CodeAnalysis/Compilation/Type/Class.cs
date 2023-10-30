﻿using LuaLanguageServer.CodeAnalysis.Compilation.Infer;
using LuaLanguageServer.CodeAnalysis.Compilation.StubIndex;
using LuaLanguageServer.CodeAnalysis.Syntax.Node;
using LuaLanguageServer.CodeAnalysis.Syntax.Node.SyntaxNodes;

namespace LuaLanguageServer.CodeAnalysis.Compilation.Type;

public class Class : LuaType, ILuaNamedType
{
    public string Name { get; }

    public Class(string name) : base(TypeKind.Class)
    {
        Name = name;
    }

    public override IEnumerable<ClassMember> GetMembers(SearchContext context)
    {
        var syntaxElement = context.Compilation
            .StubIndexImpl.ShortNameIndex.Get<LuaShortName.Class>(Name).FirstOrDefault()?.ClassSyntax;
        if (syntaxElement is null)
        {
            yield break;
        }

        var memberIndex = context.Compilation.StubIndexImpl.Members;
        foreach (var classField in memberIndex.Get<LuaMember.ClassDocField>(syntaxElement))
        {
            var member = context.InferMember(classField.ClassDocFieldSyntax, () =>
            {
                var field = classField.ClassDocFieldSyntax;
                return field switch
                {
                    { IsIntegerField: true, IntegerField: { } integerField } => new ClassMember(
                        new IndexKey.Integer(integerField.IntegerValue), field, this),
                    { IsStringField: true, StringField: { } stringField } => new ClassMember(
                        new IndexKey.String(stringField.InnerString), field, this),
                    { IsNameField: true, NameField: { } nameField } => new ClassMember(
                        new IndexKey.String(nameField.RepresentText), field, this),
                    { IsTypeField: true, TypeField: { } typeField } => new ClassMember(
                        new IndexKey.Ty(context.Infer(typeField)), field, this),
                    _ => null
                };
            });

            if (member is not null)
            {
                yield return member;
            }
        }

        // TODO attached node
        // if (syntaxElement.Parent is LuaCommentSyntax { Owner: { } attached })
        // {
        //     foreach (var attachField in memberIndex.Get<LuaMember.TableField>(attached))
        //     {
        //     }
        //
        //     foreach (var indexField in memberIndex.Get<LuaMember.Index>(attached))
        //     {
        //     }
        // }
    }

    public ILuaType GetSuper(SearchContext context)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Interface> GetInterfaces(SearchContext context)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// contains all interfaces
    /// </summary>
    public IEnumerable<Interface> GetAllInterface(SearchContext context)
    {
        throw new NotImplementedException();
    }
}

public class ClassMember : LuaTypeMember
{
    public IndexKey Key { get; }

    public LuaSyntaxElement SyntaxElement { get; }

    public ClassMember(IndexKey key, LuaSyntaxElement syntaxElement, Class containingType) : base(containingType)
    {
        Key = key;
        SyntaxElement = syntaxElement;
    }

    public override ILuaType? GetType(SearchContext context)
    {
        return SyntaxElement switch
        {
            LuaDocTypedFieldSyntax typeField => context.Infer(typeField.Type),
            LuaDocFieldSyntax field => context.Infer(field.Type),
            _ => null
        };
    }

    public override bool MatchKey(IndexKey key, SearchContext context)
    {
        return (key, Key) switch
        {
            (IndexKey.Integer i1, IndexKey.Integer i2) => i1.Value == i2.Value,
            (IndexKey.String s1, IndexKey.String s2) => s1.Value == s2.Value,
            (IndexKey.Ty t1, IndexKey.Ty t2) => t1.Value == t2.Value,
            _ => false
        };
    }
}