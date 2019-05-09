using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PascalABCCompiler.Parsers;

/*
    ProgramScopeSyntax
        BlockScopeSyntax
            ProcScopeSyntax (name)
                ParamsScopeSyntax
                    BlockScopeSyntax
                        StatListScopeSyntax (0)
                            StatListScopeSyntax (1)
            StatListScopeSyntax (0)
                StatListScopeSyntax (1)
*/

namespace PascalABCCompiler.SyntaxTree
{
    public enum SymKind { var, constant, field, param, procname, funcname, classname, recordname, interfacename,
                          unitname, templatename, property, enumname, enumerator, typesynonym};

    [Flags]
    public enum Attributes { class_attr = 1, varparam_attr = 2, override_attr = 4, public_attr = 8 };

    public class SymInfoSyntax
    {
        public override string ToString()
        {
            string typepart = "";
            if (SK == SymKind.var || SK == SymKind.field || SK == SymKind.constant || SK == SymKind.param)
                typepart = ": " + (Td == null ? "NOTYPE" : Td.ToString());
            typepart = typepart.Replace("PascalABCCompiler.SyntaxTree.", "");
            var attrstr = Attr != 0 ? "[" + Attr.ToString() + "]" : "";
            var s = "(" + Id.ToString() + "{" + SK.ToString() + "}" + typepart + attrstr + ")" + $"({Pos.line}, {Pos.column})";
            return s;
        }
        public ident Id { get; set; }
        public type_definition Td { get; set; }
        public SymKind SK { get; set; }
        public Attributes Attr { get; set; }
        public Position Pos { get; set; }
        public SymInfoSyntax(ident Id, SymKind SK, Position Pos, type_definition Td = null, Attributes Attr = 0)
        {
            this.Id = Id;
            this.Td = Td;
            this.SK = SK;
            this.Attr = Attr;
            this.Pos = Pos;
        }
        public void AddAttribute(Attributes attr)
        {
            Attr |= attr;
        }
    }

    public class ScopeSyntax
    {
        public ScopeSyntax Parent { get; set; }
        public Position Pos { get; set; }
        public List<ScopeSyntax> Children = new List<ScopeSyntax>();
        public List<SymInfoSyntax> Symbols = new List<SymInfoSyntax>();
        public ScopeSyntax(Position Pos) => this.Pos = Pos;
        public override string ToString() => GetType().Name.Replace("Syntax", "");
    }
    public class ScopeWithDefsSyntax : ScopeSyntax
    {
        public ScopeWithDefsSyntax(Position Pos) : base(Pos) { }
    } // 
    public class GlobalScopeSyntax : ScopeWithDefsSyntax // program_module unit_module
    {
        public GlobalScopeSyntax(Position Pos) : base(Pos) { }
    }

    public class NamedScopeSyntax : ScopeWithDefsSyntax
    {
        public ident Name { get; set; }
        public NamedScopeSyntax(ident Name, Position Pos) : base(Pos) => this.Name = Name;
        public override string ToString() => base.ToString() + "(" + Name + ")" + $"({Pos.line} — {Pos.end_line})";
    }

    public class ProcScopeSyntax : NamedScopeSyntax // procedure_definition
    {
        public ident ClassName { get; set; }
        public ProcScopeSyntax(ident Name, Position Pos, ident ClassName = null)
            : base(Name, Pos) => this.ClassName = ClassName;
    }
    public class ParamsScopeSyntax : ScopeSyntax // formal_parameters
    {
        public ParamsScopeSyntax(Position Pos) : base(Pos) { }
    }
    public class TypeScopeSyntax : NamedScopeSyntax
    {
        public TypeScopeSyntax(ident Name, Position Pos) : base(Name, Pos) { }
    }
    public class ClassScopeSyntax : TypeScopeSyntax
    {
        public ClassScopeSyntax(ident Name, Position Pos) : base(Name, Pos) { }
    } // 
    public class RecordScopeSyntax : TypeScopeSyntax
    {
        public RecordScopeSyntax(ident Name, Position Pos) : base(Name, Pos) { }
    } // 
    public class InterfaceScopeSyntax : TypeScopeSyntax
    {
        public InterfaceScopeSyntax(ident Name, Position Pos) : base(Name, Pos) { }
    }
    public class EnumScopeSyntax : TypeScopeSyntax
    {
        public EnumScopeSyntax(ident Name, Position Pos) : base(Name, Pos) { }
    }
    public class TypeSynonymScopeSyntax : TypeScopeSyntax
    {
        public TypeSynonymScopeSyntax(ident Name, Position Pos) : base(Name, Pos) { }
    }
    public class LightScopeSyntax : ScopeSyntax // предок всех легковесных
    {
        public LightScopeSyntax(Position Pos) : base(Pos) { }

        public override string ToString()
        {
            var name = base.ToString();
            var level = 0;
            ScopeSyntax t = this;
            while (t.Parent is LightScopeSyntax)
            {
                level++;
                t = t.Parent;
            }
            return name+ "("+ level + ")" + $"{Pos.line} {Pos.end_line}";
        }
    }
    public class StatListScopeSyntax : LightScopeSyntax // statement_list
    {
        public StatListScopeSyntax(Position Pos) : base(Pos) { }
    }
    public class RepeatScopeSyntax : LightScopeSyntax // statement_list
    {
        public RepeatScopeSyntax(Position Pos) : base(Pos) { }
    }
    public class CaseScopeSyntax : LightScopeSyntax // statement_list
    {
        public CaseScopeSyntax(Position Pos) : base(Pos) { }
    }
    public class ForScopeSyntax : LightScopeSyntax // statement_list
    {
        public ForScopeSyntax(Position Pos) : base(Pos) { }
    }
    public class ForeachScopeSyntax : LightScopeSyntax // statement_list
    {
        public ForeachScopeSyntax(Position Pos) : base(Pos) { }
    }
    public class IfScopeSyntax : LightScopeSyntax // statement_list
    {
        public IfScopeSyntax(Position Pos) : base(Pos) { }
    }
    public class WhileScopeSyntax : LightScopeSyntax // statement_list
    {
        public WhileScopeSyntax(Position Pos) : base(Pos) { }
    }
    public class LoopScopeSyntax : LightScopeSyntax // statement_list
    {
        public LoopScopeSyntax(Position Pos) : base(Pos) { }
    }
    public class WithScopeSyntax : LightScopeSyntax // statement_list
    {
        public WithScopeSyntax(Position Pos) : base(Pos) { }
    }
    public class LockScopeSyntax : LightScopeSyntax // statement_list
    {
        public LockScopeSyntax(Position Pos) : base(Pos) { }
    }
    public class SwitchScopeSyntax : LightScopeSyntax // statement_list
    {
        public SwitchScopeSyntax(Position Pos) : base(Pos) { }
    }
    public class LambdaScopeSyntax : ScopeSyntax
    {
        public LambdaScopeSyntax(Position Pos) : base(Pos) { }
    }
}

