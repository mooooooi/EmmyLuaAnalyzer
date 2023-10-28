﻿using LuaLanguageServer.CodeAnalysis.Compilation.Infer;
using LuaLanguageServer.CodeAnalysis.Syntax.Node.SyntaxNodes;

namespace LuaLanguageServer.CodeAnalysis.Compilation.Type;

public class TableStruct : LuaType
{
    public LuaDocTableTypeSyntax Table { get; }

    public TableStruct(LuaDocTableTypeSyntax table) : base(TypeKind.Table)
    {
        Table = table;
    }

    public override IEnumerable<LuaTypeMember> GetMembers(SearchContext context)
    {
        foreach (var field in Table.FieldList)
        {
            if (field.IsIntegerKey)
            {
                yield return new TableStructMember(
                    new IndexKey.Integer(field.IntegerKey!.IntegerValue), field, this);
            }
            else if (field.IsStringKey)
            {
                yield return new TableStructMember(
                    new IndexKey.String(field.StringKey!.InnerString), field, this);
            }
            else if (field.IsTypeKey)
            {
                yield return new TableStructMember(
                    new IndexKey.Ty(context.Infer(field.TypeKey)), field, this);
            }
            else if (field.IsNameKey)
            {
                yield return new TableStructMember(
                    new IndexKey.String(field.NameKey!.RepresentText), field, this);
            }
        }
    }
}

public class TableStructMember : LuaTypeMember
{
    public IndexKey Key { get; }

    public LuaDocTypedFieldSyntax Field { get; }

    public TableStructMember(IndexKey key, LuaDocTypedFieldSyntax field, TableStruct? containingType) : base(
        containingType)
    {
        Key = key;
        Field = field;
    }

    public override ILuaType GetType(SearchContext context)
    {
        return context.Infer(Field.Type);
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
