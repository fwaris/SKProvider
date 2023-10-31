namespace SKProviderRuntime
open System
open System.IO
open System.Reflection
open FSharp.Quotations
open FSharp.Core.CompilerServices
open SKProvider
open ProviderImplementation.ProvidedTypes
open Microsoft.SemanticKernel
open FSharp.Quotations.Patterns
open Microsoft.SemanticKernel.Orchestration
open FSharp.Reflection

type ImportedFunction = {Folder:string; Skill:string; FunctionName:string}//open Microsoft.SemanticKernel.Connectors.AI.OpenAI
// Put any utility helpers here
[<AutoOpen>]
module RuntimeHelper  =    

    let namedValue (ex:Expr) =
        match ex with 
        | Var v -> v.Name, Expr.Cast<string>(ex)
        | x     -> failwith $"Unrecognized pattern %A{x}"

    let namedValues (args:Expr list) = (([],<@ [] @>),args) ||> List.fold (fun (ns,es) ex -> let n,e =  namedValue ex in (n::ns),<@ %e :: %es @>)

    let ensureImportedFunction (k:IKernel) (fn:ImportedFunction) = 
        match k.Functions.TryGetFunction(fn.FunctionName) with
        | true,_ -> ()
        | _      -> k.ImportSemanticFunctionsFromDirectory(fn.Folder,fn.Skill) |> ignore    

    // let defaultConfig() = 
    //     OpenAIRequestSettings(MaxTokens=200, Temperature=0.0)    


    let ensureFunction (k:IKernel) name template =
        match k.Functions.TryGetFunction(name) with
        | true,fn -> fn
        | _      -> k.CreateSemanticFunction(template,functionName=name)

    let setContext (ctx:SKContext) varNames varVals = 
        List.zip varNames varVals 
        |> List.iter (fun (n,v) -> ctx.Variables.Add(n,v))

