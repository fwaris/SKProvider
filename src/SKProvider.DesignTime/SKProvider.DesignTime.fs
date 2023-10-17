module SKProviderImplementation
open System
open System.IO
open System.Reflection
open FSharp.Quotations
open FSharp.Core.CompilerServices
open SKProvider
open ProviderImplementation.ProvidedTypes

// Put any utility helpers here
[<AutoOpen>]
module internal Helpers =
    open Microsoft.SemanticKernel
    open Microsoft.SemanticKernel.Plugins

    let ignoreCase = StringComparison.InvariantCultureIgnoreCase

    let invokeTemplate (args:Expr list) : Expr =
        <@@
            fun ks -> async{return ks}
        @@>

    let buildKerlet name promptTemplate = 
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
        let vnames = vnames @ ["replaceTemplateWith"]
        let parms = vnames |> List.map(fun v -> ProvidedParameter(v,typeof<string>,optionalValue=null))
        let doc = fun () -> 
            let fnameStr = String.Join(",",fnames)
            let vnameStr = String.Join(",",vnames)
            match vnames,fnames with 
            | [],[] -> "No functions or variable names found"
            | [],_  -> sprintf "Functions: %s" fnameStr
            | _,[]  -> sprintf "Variables: %s" vnameStr
            | _     -> sprintf "Variables: %s, Functions: %s" fnameStr vnameStr
        let m = ProvidedMethod(name,parms,typeof<Kerlet>,isStatic=true,invokeCode=invokeTemplate)
        m.AddXmlDocDelayed doc
        m

    let addTemplate (ty:ProvidedTypeDefinition) template =
        ty.AddMember(buildKerlet "kerlet" template)

    let addSKFunction (ty:ProvidedTypeDefinition) folder (skf:ISKFunction) =         
        let path = Path.Combine(folder,skf.PluginName,skf.Name,"skprompt.txt")
        let template = File.ReadAllText(path)
        let mName = sprintf "%s_%s" skf.PluginName skf.Name
        let m = buildKerlet mName template
        ty.AddMember(m)
        
    let addFunctionsFromFolder (ty:ProvidedTypeDefinition) folder skills =
        let k = Kernel.Builder.Build()
        let skills = k.ImportSemanticFunctionsFromDirectory(folder, (Seq.toArray skills) )
        if skills.Count = 0 then failwith "No skills found. Note skills are structured as: parent folder -> 1 or more skills folders -> 1 or more function folders"        
        skills |> Seq.iter(fun kv -> addSKFunction  ty folder kv.Value)
        
[<TypeProvider>]
type SKTypeProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, assemblyReplacementMap=[("SKProvider.DesignTime", "SKProvider")])

    let ns = "SKProvider"
    let asm = Assembly.GetExecutingAssembly()

    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<Utilities.DummyType>.Assembly.GetName().Name = asm.GetName().Name)  

    let parseSkillNames str =
        if System.String.IsNullOrWhiteSpace str then 
            []
        else
            str.Split([|','|], System.StringSplitOptions.RemoveEmptyEntries) |> Array.toList

    let createType typeName (args:obj[]) =
        let template = unbox<string> args.[0]
        let skills = unbox<string> args.[1] |> parseSkillNames
        let asm = ProvidedAssembly()
        let myType = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>,isErased=false)

        let folder = Environment.ExpandEnvironmentVariables(template)
        if Directory.Exists(folder) then 
            addFunctionsFromFolder myType folder skills
        else
            addTemplate myType template

        asm.AddTypes [ myType ]

        myType

    //main provider type
    let staticParms = 
        let prompt =ProvidedStaticParameter("PromptTemplate", typeof<string>)
        let funcs = ProvidedStaticParameter("Skills",typeof<string>,parameterDefaultValue="")
        [prompt;funcs]

    let tdoc () = "Prompt template literal string or reference to a skills parent folder; Skills: list of skills to load (should not be empty if skills folder specified)"

    let myParamType = 
        let t = ProvidedTypeDefinition(asm, ns, "FuncProvider", Some typeof<obj>,isErased=false)
        t.DefineStaticParameters(staticParms, createType)
        t.AddXmlDocDelayed(tdoc)
        t
    do
        this.AddNamespace(ns, [myParamType])

