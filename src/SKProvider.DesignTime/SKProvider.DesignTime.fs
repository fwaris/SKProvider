namespace SKProviderDesign
open System
open System.IO
open System.Reflection
open FSharp.Quotations
open FSharp.Core.CompilerServices
open SKProvider
open ProviderImplementation.ProvidedTypes
open Microsoft.SemanticKernel
open FSharp.Quotations.Patterns
//open Microsoft.SemanticKernel.Connectors.AI.OpenAI
        
[<TypeProvider>]
type SKTypeProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, assemblyReplacementMap=[("SKProvider.DesignTime", "SKProvider")])

    let ns = "SKProvider"
    let asm = Assembly.GetExecutingAssembly()

    // check we contain a copy of runtime files, and are not referencing the runtime DLL
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
            Generator.addFunctionsFromFolder myType folder skills
        else
            Generator.addTemplate myType template

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

