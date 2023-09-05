module SKProviderImplementation

open System
open System.Collections.Generic
open System.IO
open System.Reflection
open FSharp.Quotations
open FSharp.Core.CompilerServices
open SKProvider
open ProviderImplementation
open ProviderImplementation.ProvidedTypes

// Put any utility helpers here
[<AutoOpen>]
module internal Helpers =
    let x = 1

    let invokeTemplate (args:Expr list) : Expr =
        <@@
            fun ks -> async{return ks}
        @@>

    let buildKerlet promptTemplate = 
        let vars = TemplateParser.extractVars promptTemplate @ ["replaceTemplateWith"]
        let parms = vars |> List.map(fun v -> ProvidedParameter(v,typeof<string>,optionalValue=null))
        ProvidedMethod("kerlet",parms,typeof<Kerlet>,isStatic=true,invokeCode=invokeTemplate)

[<TypeProvider>]
type SKTypeProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, assemblyReplacementMap=[("SKProvider.DesignTime", "SKProvider.Runtime")])

    let ns = "SKProvider"
    let asm = Assembly.GetExecutingAssembly()

    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<KState>.Assembly.GetName().Name = asm.GetName().Name)  

    let createType typeName (template:string) =
        let asm = ProvidedAssembly()
        let myType = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>,isErased=false)

        myType.AddMember(buildKerlet template)
        asm.AddTypes [ myType ]

        myType

    let myParamType = 
        let t = ProvidedTypeDefinition(asm, ns, "FuncProvider", Some typeof<obj>,isErased=false)
        t.DefineStaticParameters( [ProvidedStaticParameter("PromptTemplate", typeof<string>)], fun typeName args -> createType typeName (unbox<string> args.[0]))
        t
    do
        this.AddNamespace(ns, [myParamType])

