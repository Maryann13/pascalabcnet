using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PascalABCCompiler.SyntaxTree;
using Position = PascalABCCompiler.Parsers.Position;

namespace CodeCompletion
{
    public class ReferenceFinder1
    {
        private expression expr;
        private ScopeSyntax global;
        private compilation_unit cu;
        private ScopeSyntax def;
        private LinkedList<ScopeSyntax> localDefs = new LinkedList<ScopeSyntax>();
        private Predicate<syntax_tree_node> Cond;
        private syntax_tree_node current;
        private string name;
        private int line, col;

        public List<Position> Positions = new List<Position>();
        public Position Def => def.Pos;

        public ReferenceFinder1(expression expr, ScopeSyntax scope, compilation_unit cu)
        {
            this.expr = expr;
            this.cu = cu;
            global = scope;
            current = cu;
        }

        private ScopeSyntax FindDef(syntax_tree_node st, string name)
        {
            ScopeSyntax def = null;
            var left = ((current as ident)?.Parent as dot_node)?.left;
            var id = (st?.Parent as method_name)?.class_name ??
                left as ident ?? st as ident;

            var tt = global.Children.Where(s => s is TypeScopeSyntax)
                    .Select(s => s as TypeScopeSyntax);
            if (id != null)
                def = tt.FirstOrDefault(t => t.Name.name == id.name) ??
                    (IsUnitName(id.name) ? global : null);
            if (def == null)
                def = tt.FirstOrDefault(t => t.Name.name == GetTypeName(left));
            return def;
        }

        private bool IsUnitName(string name)
            => name != null && global.Symbols.Exists(s =>
                s.SK == SymKind.unitname && s.Id.name == name);

        private ScopeSyntax FindDef(int line, string name, ScopeSyntax cur_sc)
        {
            var ss = cur_sc.Children.FirstOrDefault(s =>
                s.Pos.line <= line && s.Pos.end_line >= line);
            if (ss != null)
            {
                var def = FindDef(line, name, ss);
                if (def != null)
                    return def;
            }

            return cur_sc.Symbols.Exists(s => s.Id.name == name) ? cur_sc : null;
        }

        private void FindLocalDefs(int line, int end_line, ScopeSyntax cur_sc)
        {
            if (cur_sc.Symbols.Exists(s => s.Id.name == name))
                localDefs.AddLast(cur_sc);

            var ss = cur_sc.Children.Where(s => s.Pos.line >= line && s.Pos.end_line <= end_line);
            foreach (var s in ss)
                FindLocalDefs(line, end_line, s);
        }

        private void GenCond(string name, int line, int col)
        {
            this.name = name;
            this.line = line;
            this.col = col;

            var dn = expr as dot_node;
            if (dn != null)
            {
                GotoIdentByLocation(name, line, col, cu);
                def = FindDef(current, name);
            }
            else
                def = FindDef(line, name, global);
            if (def == null)
                return;

            int ln = -1, end_ln = -1;
            if (def is TypeScopeSyntax)
            {
                ln = def.Pos.line;
                end_ln = def.Parent.Pos.end_line;
                FindLocalDefs(ln, def.Pos.end_line, def.Parent);
            }
            else
            {
                ln = def.Symbols.FirstOrDefault(s => s.Id.name == name)?.Pos.line ?? 0;
                end_ln = def.Pos.end_line;
                FindLocalDefs(ln, end_ln, def);
            }
            if (localDefs.Count > 0)
                localDefs.RemoveFirst();
            localDefs.Remove(localDefs.FirstOrDefault(s =>
                (s as NamedScopeSyntax)?.Name.name == (def as NamedScopeSyntax)?.Name.name));

            var cond = new LinkedList<Predicate<syntax_tree_node>>();
            cond.AddLast(stn => stn.line() >= ln && stn.end_line() <= end_ln);

            if (def is TypeScopeSyntax type)
                cond.AddLast(stn => stn.Parent is dot_node d
                        && stn == d.right && GetTypeName(d.left) == type.Name.name
                    || stn.end_line() <= def.Pos.end_line
                    || stn.Parent is method_name mn && mn?.class_name?.name == type.Name.name);
            else
                cond.AddLast(stn => !(stn.Parent is dot_node d) || stn == d.left
                    || IsUnitName((d.left as ident)?.name));

            var conds = localDefs.Select(ss => LocalDefCond(ss))
                .Concat(cond)
                .Aggregate((c1, c2) => stn => c1(stn) && c2(stn));
            Cond = conds;
        }

        private Predicate<syntax_tree_node> LocalDefCond(ScopeSyntax ss)
            => stn => stn.line() < ss.Pos.line || stn.end_line() > ss.Pos.end_line;

        private void GotoIdentByLocation(string name, int line, int col, syntax_tree_node cur)
        {
            if (cur == null)
                return;
            if (cur.line() == line && (cur as ident)?.name == name)
            {
                current = cur;
                return;
            }

            var nodes = cur.DescendantNodes();
            if (cur.line() == line && cur.end_line() == line)
                nodes = nodes?.Where(n => n != null
                    && n.column() <= col && n.end_column() >= col);
            else
                nodes = nodes?.Where(n => n != null
                    && n.line() <= line && n.end_line() >= line);

            if (nodes != null)
                foreach (var n in nodes)
                    GotoIdentByLocation(name, line, col, n);
        }

        private string GetTypeName(syntax_tree_node st)
        {
            ident id;
            string tn = null;
            if (st is dot_node dn)
            {
                tn = GetTypeName(dn?.left);
                if (tn == null)
                    return null;

                if (dn?.left == null)
                    return tn;
                else
                    id = dn?.right as ident;
            }
            else
                id = st as ident;
            if (id == null)
                return null;

            var d = FindDef(id.line(), id.name, tn == null ? global
                : global.Children.Find(s => s is NamedScopeSyntax ns && ns.Name.name == tn));
            var td = d.Symbols.Find(s => s.Id.name == id.name).Td;
            if (td != null)
                return td.ToString().Replace("PascalABCCompiler.SyntaxTree.", "");

            var pos = d.Symbols.Find(s => s.Id.name == id.name).Pos;
            GotoIdentByLocation(id.name, pos.line, pos.column, cu);
            return GetTypeName((current?.Parent?.Parent as var_def_statement)
                ?.inital_value);
        }

        public void FindPositions(string name, int line, int col, syntax_tree_node stn)
        {
            GenCond(name, line + 1, col + 1);
            FindPositionsRec(stn);
            Positions = Positions.Distinct().ToList();
        }

        private void FindPositionsRec(syntax_tree_node stn)
        {
            if (stn == null)
                return;

            switch (stn)
            {
                case ident i:
                    if (i.name == name && Cond(i))
                        Positions.Add(i.position());
                    break;
            }

            for (int i = 0; i < stn.subnodes_count; ++i)
                FindPositionsRec(stn[i]);
        }

        public void Output(string fname)
        {
#if DEBUG
            if (def == null)
                return;
            CollectLightSymInfoVisitor cv = new CollectLightSymInfoVisitor();
            cv.Root = def;
            cv.Output(fname);
            System.IO.File.WriteAllLines(@"C:\PABCWork.NET\Debug.txt", Positions.Select(p =>
                $"{name}, {p.line}, {p.column}"));
#endif
        }
    }
}
