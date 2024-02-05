﻿using EmmyLua.CodeAnalysis.Compilation.Infer;
using EmmyLua.CodeAnalysis.Compilation.Symbol;

namespace EmmyLua.CodeAnalysis.Compilation.Type;

public class LuaUnion() : LuaType(TypeKind.Union)
{
    private HashSet<ILuaType> ChildrenType { get; } = new();

    private static bool IsValid(ILuaType ty)
    {
        return ty is Unknown;
    }

    public static ILuaType UnionType(ILuaType a, ILuaType b)
    {
        if (IsValid(a))
        {
            return b;
        }
        else if (IsValid(b))
        {
            return a;
        }
        else if (a is LuaUnion unionType)
        {
            unionType.ChildrenType.Add(b);
            return unionType;
        }
        else if (b is LuaUnion unionType2)
        {
            unionType2.ChildrenType.Add(a);
            return unionType2;
        }
        else
        {
            var union = new LuaUnion(a, b);
            return union.ChildrenType.Count == 1 ? union.ChildrenType.First() : union;
        }
    }

    public static void Process(ILuaType symbol, Func<ILuaType, bool> process)
    {
        if (symbol is LuaUnion unionSymbol)
        {
            foreach (var childSymbol in unionSymbol.ChildrenType)
            {
                if (!process(childSymbol))
                {
                    break;
                }
            }
        }
        else
        {
            process(symbol);
        }
    }

    public static void Each(ILuaType symbol, Action<ILuaType> action)
    {
        Process(symbol, s =>
        {
            action(s);
            return true;
        });
    }

    public LuaUnion(ILuaType a, ILuaType b) : this()
    {
        ChildrenType.Add(a);
        ChildrenType.Add(b);
    }

    public ILuaType UnionType(ILuaType symbol)
    {
        if (symbol is LuaUnion unionSymbol)
        {
            foreach (var childSymbol in unionSymbol.ChildrenType)
            {
                ChildrenType.Add(childSymbol);
            }
        }
        else
        {
            ChildrenType.Add(symbol);
        }

        return this;
    }

    protected override bool OnSubTypeOf(ILuaType other, SearchContext context)
    {
        if (other is LuaUnion otherUnion)
        {
            return ChildrenType.All(it => otherUnion.ChildrenType.Any(it2 => it.SubTypeOf(it2, context)));
        }

        return false;
    }

    public override string ToDisplayString(SearchContext context)
    {
        return string.Join("|", ChildrenType.Select(it => it.ToDisplayString(context)));
    }

    public override bool IsNullable => ChildrenType.Any(it => it.IsNullable);
}

public static class UnionTypeExtensions
{
    public static ILuaType Union(this ILuaType a, ILuaType b)
    {
        return LuaUnion.UnionType(a, b);
    }
}
