﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
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
        private string name, new_val;
        private int line, column;

        public List<Position> Positions = new List<Position>();
        public Position Def => def.Pos;

        public ReferenceFinder1(expression expr, ScopeSyntax scope, compilation_unit cu, string new_val)
        {
            this.expr = expr;
            this.cu = cu;
            global = scope;
            current = cu;
            this.new_val = new_val;
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
            if (cur_sc == null)
                return null;
            var ss = cur_sc.Children.FirstOrDefault(s =>
                s.Pos.line <= line && s.Pos.end_line >= line);
            if (ss != null)
            {
                var def = FindDef(line, name, ss);
                if (def != null)
                    return def;
            }

            var def_base_type = cur_sc is TypeScopeSyntax ?
                FindDefBaseType(name, cur_sc) : null;
            return def_base_type ?? (cur_sc.Symbols.Exists(s => s.Id.name == name) ? cur_sc : null);
        }

        private ScopeSyntax FindDefBaseType(string name, ScopeSyntax cur_sc)
        {
            ScopeSyntax found_sc = null;
            if (cur_sc is TypeScopeSyntax && !(cur_sc is EnumScopeSyntax))
            {
                found_sc = cur_sc;
                var sdef = found_sc.Symbols.FirstOrDefault(s => s.Id.name == name);
                while (sdef == null || sdef.Attr.HasFlag(Attributes.override_attr))
                {
                    found_sc = global.Children.FirstOrDefault(s => s is TypeScopeSyntax ts
                        && ((((found_sc as TypeScopeSyntax).Name.Parent as type_declaration)
                            ?.type_def as class_definition)?.class_parents?.types?.Exists(c =>
                                c.names.Exists(n => n.name == ts.Name.name)) ?? false));
                    if (found_sc == null)
                        break;
                    sdef = found_sc.Symbols.FirstOrDefault(s => s.Id.name == name);
                }
            }
            return found_sc;
        }

        private void FindLocalDefs(int line, int end_line, ScopeSyntax cur_sc)
        {
            if (cur_sc.Symbols.Exists(s => s.Id.name == name)
                    && !(cur_sc is EnumScopeSyntax))
                localDefs.AddLast(cur_sc);

            var ss = cur_sc.Children.Where(s => s.Pos.line >= line && s.Pos.end_line <= end_line);
            foreach (var s in ss)
                FindLocalDefs(line, end_line, s);
        }

        private void GenCond(string name, int line, int column)
        {
            this.name = name;
            this.line = line;
            this.column = column;

            var dn = expr as dot_node;
            if (dn != null)
            {
                GotoIdentByLocation(name, line, column, cu);
                def = FindDef(current, name);
            }
            else
                def = FindDef(line, name, global);
            CheckNewValDoesNotExist();

            if (def == null)
                foreach (var e in global.Children.Where(s => s is EnumScopeSyntax))
                    if (e.Symbols.Exists(s => s.Id.name == name))
                    {
                        def = e;
                        break;
                    }
            if (def == null)
                return;

            var defs = def.Symbols.FindAll(s => s.Id.name == name);
            var conflict_defs = defs.Count > 1 ?
                defs.Skip(1).Where(d => d.SK != SymKind.funcname && d.SK != SymKind.procname) :
                new List<SymInfoSyntax>();

            foreach (var d in conflict_defs)
                if (d.Pos.line == line
                    && d.Pos.column <= column && column <= d.Pos.end_column)
                {
                    Cond = s => s.line() == d.Pos.line && s.column() == d.Pos.column;
                    return;
                }

            int ln = def.Pos.line, col = def.Pos.column,
                end_ln = -1, end_col = -1;
            if (def is TypeScopeSyntax)
            {
                end_ln = def.Parent.Pos.end_line;
                end_col = def.Parent.Pos.end_column;
                FindLocalDefs(ln, def.Pos.end_line, def.Parent);
            }
            else
            {
                end_ln = def.Pos.end_line;
                end_col = def.Pos.end_column;
                FindLocalDefs(ln, end_ln, def);
            }
            if (localDefs.Count > 0)
                localDefs.RemoveFirst();
            localDefs.Remove(localDefs.FirstOrDefault(s => s is NamedScopeSyntax ns
                && ns?.Name.name == (def as NamedScopeSyntax)?.Name.name));

            var cond = new LinkedList<Predicate<syntax_tree_node>>();
            cond.AddLast(stn => In(stn, ln, col, end_ln, end_col));

            foreach (var d in conflict_defs)
                cond.AddLast(stn => stn.line() != d.Pos.line
                    || stn.column() != d.Pos.column);

            if (def is EnumScopeSyntax es)
                cond.AddLast(stn => !(stn.Parent is dot_node d)
                    || stn == d.right && (d.left as ident)?.name == es.Name.name);
            else if (def is TypeScopeSyntax type)
            {
                var in_derived_cond = GenCondDerivedTypes();
                cond.AddLast(stn => stn.Parent is dot_node d
                        && stn == d.right && GetTypeName(d.left) == type.Name.name
                    || stn.end_line() <= def.Pos.end_line
                    || in_derived_cond(stn)
                    || stn.Parent is method_name mn && mn?.class_name?.name == type.Name.name);
            }
            else
                cond.AddLast(stn => !(stn.Parent is dot_node d) || stn == d.left
                    || IsUnitName((d.left as ident)?.name));

            var conds = localDefs.Select(ss => LocalDefCond(ss))
                .Concat(cond)
                .Aggregate((c1, c2) => stn => c1(stn) && c2(stn));
            Cond = conds;
        }

        private Predicate<syntax_tree_node> GenCondDerivedTypes()
        {
            if (def is TypeScopeSyntax type)
            {
                var in_derived_conds = new List<Predicate<syntax_tree_node>>();
                var scopes = new List<TypeScopeSyntax>();
                scopes.Add(type);
                List<TypeScopeSyntax> derived_types;
                do
                {
                    derived_types = new List<TypeScopeSyntax>();
                    foreach (var sc in scopes)
                        derived_types.AddRange(global.Children.Where(s => s is TypeScopeSyntax ts
                                && (((ts.Name.Parent as type_declaration)?.type_def as class_definition)
                                    ?.class_parents?.types?.Exists(c =>
                                        c.names.Exists(n => n.name == sc.Name.name)
                                && !s.Symbols.Exists(sym => sym.Id.name == name
                                    && !sym.Attr.HasFlag(Attributes.override_attr))) ?? false))
                            .Select(s => s as TypeScopeSyntax));

                    in_derived_conds.AddRange(derived_types.Select(t => (Predicate<syntax_tree_node>)
                            (stn => In(stn, t.Pos.line, t.Pos.column, t.Pos.end_line, t.Pos.end_column))));
                    scopes = derived_types;
                } while (derived_types.Count > 0);

                return in_derived_conds.Count > 0 ?
                    in_derived_conds.Aggregate((c1, c2) =>
                        stn => c1(stn) || c2(stn)) :
                    stn => false;
            }
            return stn => false;
        }

        private void CheckNewValDoesNotExist()
        {
            var cur = def;
            while (cur != null && !(cur is TypeScopeSyntax))
            {
                if (cur.Symbols.Exists(s => s.Id.name == new_val))
                {
                    string sc_str = cur == def ? "В данной" : "Во включающей";
                    MessageBox.Show($"{sc_str} области локальной видимости уже содержится " +
                        "переменная с таким именем!", "Неразрешимый конфликт", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    def = null;
                    return;
                }
                cur = cur.Parent;
            }
        }

        private bool In(syntax_tree_node stn, int ln, int col, int end_ln, int end_col)
            => stn.line() >= ln && stn.end_line() < end_ln
                || ln == end_ln && stn.line() == ln
                    && stn.column() >= col && stn.column() <= end_col
                || ln != end_ln && (stn.line() == ln && stn.column() >= col
                    || stn.line() == end_ln && stn.column() <= end_col);

        private bool In(syntax_tree_node stn, Position pos)
            => In(stn, pos.line, pos.column, pos.end_line, pos.end_column);

        private Predicate<syntax_tree_node> LocalDefCond(ScopeSyntax ss)
            => stn => !(In(stn, ss.Pos.line, ss.Pos.column,
                ss.Pos.end_line, ss.Pos.end_column));

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
            {
                string type_name = td.Parent is type_declaration tdecl ?
                    tdecl.type_name.name :
                    td.ToString();
                return type_name.Replace("PascalABCCompiler.SyntaxTree.", "");
            }

            var pos = d.Symbols.Find(s => s.Id.name == id.name).Pos;
            GotoIdentByLocation(id.name, pos.line, pos.column, cu);
            return GetTypeName((current?.Parent?.Parent as var_def_statement)
                ?.inital_value);
        }

        public void FindPositions(string name, int line, int col, syntax_tree_node stn)
        {
            GenCond(name, line + 1, col + 1);
            if (def == null)
                return;
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
            var positions = string.Join(", ", Positions.Select(p => $"{p.line} {p.column}"));
            System.IO.File.AppendAllText(fname,
                $"{expr.ToString()} {name} {line} {column} {new_val}\n{positions}\n;\n");
#endif
        }
    }
}
