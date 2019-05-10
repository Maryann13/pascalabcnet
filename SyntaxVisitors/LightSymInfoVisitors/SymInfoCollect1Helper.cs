using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PascalABCCompiler.Parsers;

/*
    GlobalScopeSyntax
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
    public partial class CollectLightSymInfoVisitor : BaseEnterExitVisitor
    {
        public void AddSymbol(ident name, SymKind kind, type_definition td = null, Attributes attr = 0)
        {
            Current.Symbols.Add(new SymInfoSyntax(name, kind, name.position(), td, attr));
        }
        public string Spaces(int n) => new string(' ', n);
        public void OutputString(string s) => System.IO.File.AppendAllText(fname, s);
        public void OutputlnString(string s = "") => System.IO.File.AppendAllText(fname, s + '\n');
        public void Output(string fname)
        {
#if DEBUG
            this.fname = fname;
            if (System.IO.File.Exists(fname))
                System.IO.File.Delete(fname);
            OutputElement(0, Root);
#endif
        }
        string fname;
        public void OutputElement(int d, ScopeSyntax s)
        {
            OutputString(Spaces(d));
            if (s == null)
                throw new Exception("ggggggggg");
            if (s is ParamsScopeSyntax)
            {
                OutputString(s.ToString()+": ");
                if (s.Symbols.Count > 0)
                    OutputlnString(string.Join(", ", s.Symbols.Select(x => x.ToString())));
                else
                    OutputlnString();
            }
            else
            {
                OutputlnString(s.ToString());
                if (s.Symbols.Count > 0)
                    OutputlnString(Spaces(d + 2) + string.Join(", ", s.Symbols.Select(x => x.ToString())));
            }
            foreach (var sc in s.Children)
                OutputElement(d + 2, sc);
        }
    }

    public static class GetPosition
    {
        public static Position position(this syntax_tree_node stn)
        {
            Position pos = new Position();
            if (stn != null && stn.source_context != null)
            {
                pos.line = stn.source_context.begin_position.line_num;
                pos.column = stn.source_context.begin_position.column_num;
                pos.end_line = stn.source_context.end_position.line_num;
                pos.end_column = stn.source_context.end_position.column_num;
                pos.file_name = stn.source_context.FileName;
            }
            return pos;
        }

        public static int line(this syntax_tree_node stn) =>
            stn?.source_context?.begin_position?.line_num ?? 0;

        public static int end_line(this syntax_tree_node stn) =>
            stn?.source_context?.end_position?.line_num ?? 0;

        public static int column(this syntax_tree_node stn) =>
            stn?.source_context?.begin_position?.column_num ?? 0;

        public static int end_column(this syntax_tree_node stn) =>
            stn?.source_context?.end_position?.column_num ?? 0;
    }

    public static class HashSetExt
    {
        public static void AddRange<TSource>
            (this HashSet<TSource> source, IEnumerable<TSource> collection)
        {
            foreach (var e in collection)
                source.Add(e);
        }
    }
}

