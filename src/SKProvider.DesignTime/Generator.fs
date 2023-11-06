namespace SKProviderDesign
open System
open System.IO
open FSharp.Quotations
open FSharp.Core.CompilerServices
open SKProvider
open ProviderImplementation.ProvidedTypes
open Microsoft.SemanticKernel
open FSharp.Reflection

[<AutoOpen>]
module internal Generator =    

    let invokeFunction name template (args:Expr list) : Expr =
        let names,exps = SKProviderRuntime.RuntimeHelper.namedValues args           
        <@            
            fun (ks:KState) -> 
                async {
                    let func = SKProviderRuntime.RuntimeHelper.ensureFunction ks.Kernel name template  
                    let ks' = SKProviderRuntime.RuntimeHelper.setContext ks names %exps
                    let! fctx = func.InvokeAsync(ks'.Kernel,variables=ks'.Context.Variables) |> Async.AwaitTask
                    return ks'      
                }           
        @>.Raw

    let invokeLoadedFunction (funcBind:Expr<SKProviderRuntime.ImportedFunction>) (args:Expr list) : Expr =
        let names,exps = SKProviderRuntime.RuntimeHelper.namedValues args          
        <@     
            fun (ks:KState) -> 
                async {
                    SKProviderRuntime.RuntimeHelper.ensureImportedFunction ks.Kernel %funcBind
                    let ks' = SKProviderRuntime.RuntimeHelper.setContext ks names %exps
                    let pluginName = (%funcBind).Skill
                    let funcName = (%funcBind).FunctionName                    
                    let func = ks.Kernel.Functions.GetFunction(pluginName,funcName)
                    let! fctx = func.InvokeAsync(ks'.Kernel,variables=ks'.Context.Variables) |> Async.AwaitTask
                    return ks'
                }            
        @>.Raw


    let buildKerlet name promptTemplate invokeCode = 
        let blocks = TemplateParser.extractVars promptTemplate
        let vnames = 
            blocks 
            |> List.choose(function 
                | TemplateParser.VarBlock v -> Some v 
                | TemplateParser.FuncBlock (_,Some v) when v.StartsWith("$") -> v.Substring(1) |> Some
                | _ -> None)
            |> List.distinct
        let fnames = 
            blocks 
            |> List.choose(function                 
                | TemplateParser.FuncBlock (n,_) -> Some n
                | _ -> None)
            |> List.distinct
        let vnamesExt = vnames @ ["template"]
        let parms = vnamesExt |> List.map(fun v -> ProvidedParameter(v,typeof<string>,optionalValue=null))
        let doc = fun () -> 
            let fnameStr = String.Join(", ",fnames)
            let vnamesDoc = vnames @ ["template (overrides the prompt template)"]
            let vnameStr = String.Join(", ",vnamesDoc)
            match vnamesExt,fnames with 
            | [],[] -> "No functions or variable names found"
            | [],_  -> sprintf "Functions: %s" fnameStr
            | _,[]  -> sprintf "Variables: %s" vnameStr
            | _     -> sprintf "Variables: %s, Functions: %s" vnameStr fnameStr
        let m = ProvidedMethod(name,parms,typeof<Kerlet>,isStatic=true,invokeCode=invokeCode)
        m.AddXmlDocDelayed doc
        m

    ///taken from SQLProvider 
    let rec coerceValues fieldTypeLookup fields = 
        Array.mapi (fun i v ->
                let expr = 
                    if isNull v then simpleTypeExpr v
                    elif FSharpType.IsRecord (v.GetType()) then recordExpr v |> snd
                    else simpleTypeExpr v
                Expr.Coerce(expr, fieldTypeLookup i)
        ) fields |> List.ofArray
        
    and simpleTypeExpr instance = Expr.Value(instance)

    and recordExpr instance = 
        let tpy = instance.GetType()
        let fields = FSharpValue.GetRecordFields(instance)
        let fieldInfo = FSharpType.GetRecordFields(tpy)
        let fieldTypeLookup indx = fieldInfo.[indx].PropertyType
        tpy, Expr.NewRecord(instance.GetType(), coerceValues fieldTypeLookup fields)


    let buildKerletFromImportedFunc (fn:SKProviderRuntime.ImportedFunction) template =
        let _,recExp = recordExpr fn
        let vExp = recExp |> Expr.Cast<SKProviderRuntime.ImportedFunction>
        buildKerlet fn.FunctionName template (invokeLoadedFunction vExp)

    let addTemplate (ty:ProvidedTypeDefinition) template =
        ty.AddMember(buildKerlet "kerlet" template (invokeFunction ty.Name template))

    let addNestedType  (ty:ProvidedTypeDefinition) (templates:string*(SKProviderRuntime.ImportedFunction*string) list) =
        let n,ts = templates
        let t1 = ProvidedTypeDefinition(n,Some typeof<obj>, isErased=false)
        for n,tpml in ts do 
            let m = buildKerletFromImportedFunc n tpml 
            t1.AddMember(m)
        ty.AddMember(t1)

    let getTemplate folder (skf:ISKFunction) =
        let path = Path.Combine(folder,skf.PluginName,skf.Name,"skprompt.txt")
        File.ReadAllText(path)

    let addFunctionsFromFolder (ty:ProvidedTypeDefinition) folder skills =
        let k = (new KernelBuilder()).Build()
        let skills = k.ImportSemanticFunctionsFromDirectory(folder, (Seq.toArray skills) )
        if skills.Count = 0 then failwith "No skills found. Note skills are structured as: parent folder -> 1 or more skills folders -> 1 or more function folders"        
        skills 
        |> Seq.groupBy(fun kv -> kv.Value.PluginName)
        |> Seq.iter(fun (k,kvs) -> 
            let ts = 
                kvs 
                |> Seq.map (fun kv -> 
                    let fn = {SKProviderRuntime.ImportedFunction.FunctionName=kv.Value.Name; SKProviderRuntime.ImportedFunction.Skill=kv.Value.PluginName; SKProviderRuntime.ImportedFunction.Folder=folder}
                    let template =  getTemplate folder kv.Value
                    fn,template)
                |> Seq.toList
            addNestedType ty (k,ts))
