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

        public static CollectLightSymInfoVisitor New => new CollectLightSymInfoVisitor();
        public override void Enter(syntax_tree_node st)
        {
            ScopeSyntax t = null;
            switch (st)
            {
                case program_module p:
                case unit_module u:
                    t = new GlobalScopeSyntax(st.position());
                    Root = t;
                    break;
                case procedure_definition pd:
                case procedure_header p when !(p.Parent is procedure_definition):
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
                        t = new ProcScopeSyntax(name, st.position(),
                            pdef?.proc_header.name?.class_name);
                    break;
                case enum_type_definition e:
                    var edecl = e.Parent as type_declaration;
                    ident ename = edecl?.type_name;
                    AddSymbol(ename, SymKind.enumname, edecl?.type_def);
                    t = new EnumScopeSyntax(ename, st.position());
                    foreach (var en in e.enumerators?.enumerators)
                    {
                        var nm = (en.name as named_type_reference)?.names[0];
                        if (nm != null)
                            t.Symbols.Add(new SymInfoSyntax(nm, SymKind.enumerator,
                                nm.position()));
                    }
                    break;
                case simple_property sp:
                    AddSymbol(sp.property_name?.name, SymKind.property);
                    break;
                case const_definition p:
                    AddSymbol(p.const_name?.name, SymKind.constant);
                    break;
                case formal_parameters p:
                    //t = new ParamsScopeSyntax(st.position());
                    break;
                case statement_list p:
                    t = new StatListScopeSyntax(st.position());
                    break;
                case for_node p:
                    t = new ForScopeSyntax(st.position());
                    break;
                case foreach_stmt p:
                    t = new ForeachScopeSyntax(st.position());
                    break;
                /*case repeat_node p: // не надо т.к. это StatListScope
                    t = new RepeatScopeSyntax();
                    break;*/
                case if_node p:
                    t = new IfScopeSyntax(st.position());
                    break;
                case while_node p:
                    t = new WhileScopeSyntax(st.position());
                    break;
                case loop_stmt p:
                    t = new LoopScopeSyntax(st.position());
                    break;
                case with_statement p:
                    t = new WithScopeSyntax(st.position());
                    break;
                case lock_stmt p:
                    t = new LockScopeSyntax(st.position());
                    break;
                case case_node p:
                    t = new CaseScopeSyntax(st.position());
                    break;
                case class_definition p:
                    var td = p.Parent as type_declaration;
                    var tname = td==null ? "NONAME" : td.type_name;
                    var sself = new SymInfoSyntax(new ident("Self"), SymKind.field,
                        p.position(), td.type_def);
                    if (p.keyword == class_keyword.Class)
                    {
                        AddSymbol(tname, SymKind.classname, td?.type_def);
                        t = new ClassScopeSyntax(tname, td.position());
                        t.Symbols.Add(sself);
                    }                        
                    else if (p.keyword == class_keyword.Record)
                    {
                        AddSymbol(tname, SymKind.recordname, td?.type_def);
                        t = new RecordScopeSyntax(tname, td.position());
                        t.Symbols.Add(sself);
                    }                        
                    else if (p.keyword == class_keyword.Interface)
                    {
                        AddSymbol(tname, SymKind.interfacename, td?.type_def);
                        t = new InterfaceScopeSyntax(tname, td.position());
                        t.Symbols.Add(sself);
                    }                        
                    break;
                case type_declaration tdecl when !(tdecl.type_def is class_definition):
                    t = new TypeSynonymScopeSyntax(tdecl.type_name, tdecl.position());
                    var q = (tdecl?.type_name as template_type_name)?.template_args
                        ?.idents?.Select(x =>
                            new SymInfoSyntax(x, SymKind.templatename, x.position()));
                    if (q != null)
                        t.Symbols.AddRange(q);
                    AddSymbol(tdecl.type_name, SymKind.typesynonym, tdecl?.type_def);
                    break;
                case function_lambda_definition p:
                    t = new LambdaScopeSyntax(st.position());
                    break;
            }
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
            switch (st)
            {
                case program_module p:
                case procedure_definition pd:
                //case formal_parameters fp:
                case statement_list stl:
                case for_node f:
                case foreach_stmt fe:
                case if_node ifn:
                case while_node w:
                case loop_stmt l:
                case with_statement ws:
                case lock_stmt ls:
                case class_definition cd:
                case type_declaration tdecl
                    when !(tdecl.type_def is class_definition):
                case enum_type_definition e:
                //case record_type rt:
                case function_lambda_definition fld:
                //case repeat_node rep:
                case case_node cas:
                    Current = Current.Parent;
                    break;
            }
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
    }
}

