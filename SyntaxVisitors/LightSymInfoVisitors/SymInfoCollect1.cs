using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    // Визитор накопления легковесной синтаксической таблицы символов. 
    // Не дописан. Имеет большой потенциал применения
    public partial class CollectLightSymInfoVisitor : BaseEnterExitVisitor
    {
        public ScopeSyntax Root;
        public ScopeSyntax Current;
        private Dictionary<Type, EnterExitAction> EnterExitActions;

        public CollectLightSymInfoVisitor()
        {
            EnterExitActions = new Dictionary<Type, EnterExitAction>();
            EnterExitActions.Add(typeof(program_module), new EnterExitAction(st =>
            {
                ScopeSyntax t = new GlobalScopeSyntax(st.position());
                Root = t;
                return t;
            },
                ExitScopeSyntax));
            EnterExitActions.Add(typeof(unit_module),
                EnterExitActions[typeof(program_module)]);
            EnterExitActions.Add(typeof(procedure_definition), new EnterExitAction(st =>
            {
                var ph = st as procedure_header
                        ?? (st as procedure_definition)?.proc_header;
                var name = ph?.name?.meth_name;
                if (ph is constructor && name == null)
                    name = "Create";
                else if (ph.Parent is type_declaration tdecl)
                    name = tdecl.type_name;
                var attr = ph?.proc_attributes?.proc_attributes?.Exists(pa =>
                        pa.attribute_type == proc_attribute.attr_override) ?? false ?
                    Attributes.override_attr : 0;
                if (ph.class_keyword)
                    attr |= Attributes.class_attr;
                if ((ph.Parent as class_members)?.access_mod?.access_level
                        == access_modifer.public_modifer)
                    attr |= Attributes.public_attr;
                var sk = ph is function_header ?
                    SymKind.funcname : SymKind.procname;
                if (name != null)
                    AddSymbol(name, sk, null, attr);

                if (st is procedure_definition pdef)
                    return new ProcScopeSyntax(name, st.position(),
                        pdef?.proc_header.name?.class_name);
                else
                    return null;
            },
                ExitScopeSyntax));
            EnterExitActions.Add(typeof(procedure_header), new EnterExitAction(st =>
            {
                if (st.Parent is procedure_definition)
                    return null;
                return EnterExitActions[typeof(procedure_definition)].Enter(st);
            }));
            EnterExitActions.Add(typeof(enum_type_definition), new EnterExitAction(st =>
            {
                var edecl = st.Parent as type_declaration;
                ident ename = edecl?.type_name;
                AddSymbol(ename, SymKind.enumname, edecl?.type_def);
                ScopeSyntax t = new EnumScopeSyntax(ename, st.position());
                foreach (var en in (st as enum_type_definition)?.enumerators?.enumerators)
                {
                    var nm = (en.name as named_type_reference)?.names[0];
                    if (nm != null)
                        t.Symbols.Add(new SymInfoSyntax(nm, SymKind.enumerator,
                            nm.position()));
                }
                return t;
            },
                ExitScopeSyntax));
            EnterExitActions.Add(typeof(simple_property), new EnterExitAction(st =>
            {
                AddSymbol((st as simple_property).property_name?.name, SymKind.property);
                return null;
            }));
            EnterExitActions.Add(typeof(const_definition), new EnterExitAction(st =>
            {
                AddSymbol((st as const_definition).const_name?.name, SymKind.constant);
                return null;
            }));
            EnterExitActions.Add(typeof(statement_list), new EnterExitAction(st =>
            {
                return new StatListScopeSyntax(st.position());
            },
                ExitScopeSyntax));
            EnterExitActions.Add(typeof(for_node), new EnterExitAction(st =>
            {
                return new ForScopeSyntax(st.position());
            },
                ExitScopeSyntax));
            EnterExitActions.Add(typeof(if_node), new EnterExitAction(st =>
            {
                return new IfScopeSyntax(st.position());
            },
                ExitScopeSyntax));
            EnterExitActions.Add(typeof(foreach_stmt), new EnterExitAction(st =>
            {
                return new ForeachScopeSyntax(st.position());
            },
                ExitScopeSyntax));
            EnterExitActions.Add(typeof(while_node), new EnterExitAction(st =>
            {
                return new WhileScopeSyntax(st.position());
            },
                ExitScopeSyntax));
            EnterExitActions.Add(typeof(loop_stmt), new EnterExitAction(st =>
            {
                return new LoopScopeSyntax(st.position());
            },
                ExitScopeSyntax));
            EnterExitActions.Add(typeof(with_statement), new EnterExitAction(st =>
            {
                return new WithScopeSyntax(st.position());
            },
                ExitScopeSyntax));
            EnterExitActions.Add(typeof(lock_stmt), new EnterExitAction(st =>
            {
                return new LockScopeSyntax(st.position());
            },
                ExitScopeSyntax));
            EnterExitActions.Add(typeof(case_node), new EnterExitAction(st =>
            {
                return new CaseScopeSyntax(st.position());
            },
                ExitScopeSyntax));
            EnterExitActions.Add(typeof(class_definition), new EnterExitAction(st =>
            {
                ScopeSyntax t = null;
                var cd = st as class_definition;
                var td = cd.Parent as type_declaration;
                var tname = td == null ? "NONAME" : td.type_name;
                var sself = new SymInfoSyntax(new ident("Self"), SymKind.field,
                    cd.position(), td.type_def);
                if (cd.keyword == class_keyword.Class)
                {
                    AddSymbol(tname, SymKind.classname, td?.type_def);
                    t = new ClassScopeSyntax(tname, td.position());
                    t.Symbols.Add(sself);
                }
                else if (cd.keyword == class_keyword.Record)
                {
                    AddSymbol(tname, SymKind.recordname, td?.type_def);
                    t = new RecordScopeSyntax(tname, td.position());
                    t.Symbols.Add(sself);
                }
                else if (cd.keyword == class_keyword.Interface)
                {
                    AddSymbol(tname, SymKind.interfacename, td?.type_def);
                    t = new InterfaceScopeSyntax(tname, td.position());
                    t.Symbols.Add(sself);
                }
                return t;
            },
                ExitScopeSyntax));
            EnterExitActions.Add(typeof(type_declaration), new EnterExitAction(st =>
            {
                var tdecl = st as type_declaration;
                if (tdecl.type_def is class_definition)
                    return null;
                ScopeSyntax t = new TypeSynonymScopeSyntax(tdecl.type_name, tdecl.position());
                var q = (tdecl?.type_name as template_type_name)?.template_args
                    ?.idents?.Select(x =>
                        new SymInfoSyntax(x, SymKind.templatename, x.position()));
                if (q != null)
                    t.Symbols.AddRange(q);
                AddSymbol(tdecl.type_name, SymKind.typesynonym, tdecl?.type_def);
                return t;
            },
            st =>
            {
                if (!((st as type_declaration)?.type_def is class_definition))
                    ExitScopeSyntax(st);
            }));
            EnterExitActions.Add(typeof(function_lambda_definition), new EnterExitAction(st =>
            {
                return new LambdaScopeSyntax(st.position());
            },
                ExitScopeSyntax));
        }

        public static CollectLightSymInfoVisitor New => new CollectLightSymInfoVisitor();
        public override void Enter(syntax_tree_node st)
        {
            ScopeSyntax t = null;

            Type typ = st.GetType();
            if (EnterExitActions.ContainsKey(typ))
                t = EnterExitActions[typ].Enter(st);
            if (t != null)
            {
                t.Parent = Current;
                if (Current != null)
                    Current.Children.Add(t);
                Current = t;
                if (st is procedure_definition p)
                {
                    if (p.proc_header is function_header fh)
                    {
                        AddSymbol(new ident("Result"), SymKind.var, fh.return_type);
                    }
                }
                if (st is procedure_definition || st is class_definition)
                {
                    var ta = st is procedure_definition pd ? pd.proc_header?.template_args
                        : ((st.Parent as type_declaration)
                            ?.type_name as template_type_name)?.template_args;
                    var q = ta?.idents?.Select(x =>
                        new SymInfoSyntax(x, SymKind.templatename, x.position()));
                    if (q != null)
                        Current.Symbols.AddRange(q);
                }
            }
        }

        public override void Exit(syntax_tree_node st)
        {
            Type typ = st.GetType();
            if (EnterExitActions.ContainsKey(typ)
                    && EnterExitActions[typ].Exit != null)
                EnterExitActions[typ].Exit(st);
        }

        public override void visit(var_def_statement vd)
        {
            var attr = vd.var_attr == definition_attribute.Static ? Attributes.class_attr : 0;
            if ((vd.Parent as class_members)?.access_mod?.access_level
                    == access_modifer.public_modifer)
                attr |= Attributes.public_attr;
            if (vd == null || vd.vars == null || vd.vars.list == null)
                return;
            var type = vd.vars_type;
            var sk = Current is TypeScopeSyntax ? SymKind.field : SymKind.var;
            var q = vd.vars.list.Select(x => new SymInfoSyntax(x, sk, x.position(), type, attr));
            if (q.Count() > 0)
                Current.Symbols.AddRange(q);
            base.visit(vd);
        }

        public override void visit(formal_parameters fp)
        {
            foreach (var pg in fp.params_list)
            {
                var type = pg.vars_type;
                var q = pg.idents.idents.Select(x => new SymInfoSyntax(x, SymKind.param, x.position(), type));
                if (Current is ProcScopeSyntax || Current is LambdaScopeSyntax)
                    Current.Symbols.AddRange(q);
            }
            base.visit(fp);
        }

        public override void visit(uses_list ul)
        {
            foreach (var u in ul.units)
            {
                var q = u.name.idents.Select(x => new SymInfoSyntax(x, SymKind.unitname, x.position()));
                Current.Symbols.AddRange(q);
            }
            base.visit(ul);
        }

        public override void visit(for_node f)
        {
            if (f.create_loop_variable || f.type_name != null)
                AddSymbol(f.loop_variable, SymKind.var, f.type_name);
            base.visit(f);
        }

        private class EnterExitAction
        {
            public Func<syntax_tree_node, ScopeSyntax> Enter;
            public Action<syntax_tree_node> Exit;

            public EnterExitAction(Func<syntax_tree_node, ScopeSyntax> Enter,
                Action<syntax_tree_node> Exit = null)
            {
                this.Enter = Enter;
                this.Exit = Exit;
            }
        }

        private void ExitScopeSyntax(syntax_tree_node st)
        {
            Current = Current.Parent;
        }
    }
}

