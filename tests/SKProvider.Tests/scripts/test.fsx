#load "packages.fsx"
open System
open Microsoft.SemanticKernel
open Microsoft.SemanticKernel.Plugins.Core
open Microsoft.SemanticKernel.Plugins.Document
open Microsoft.SemanticKernel.Plugins.Document.OpenXml
open SKProvider
open SKProvider.Ops
open Microsoft.Extensions.Logging

let logger = 
    {new ILogger with
            member this.BeginScope(state) = raise (System.NotImplementedException())
            member this.IsEnabled(logLevel) = true
            member this.Log(logLevel, eventId, state, ``exception``, formatter) = 
                let msg = formatter.Invoke(state,``exception``)
                printfn "Kernel: %s" msg
    }

let loggerFactory = 
    {new ILoggerFactory with
            member this.AddProvider(provider) = ()
            member this.CreateLogger(categoryName) = logger
            member this.Dispose() = ()
    }

let kstate() = 
    let kernel = 
        (KernelBuilder())
            .WithOpenAIChatCompletionService("gpt-3.5-turbo",Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
            .WithLoggerFactory(loggerFactory)
            .Build()
    let fns = kernel.ImportFunctions(TimePlugin(),"timePlugin")
    let fns2 = kernel.ImportFunctions(DocumentPlugin(WordDocumentConnector(),FileSystem.LocalFileSystemConnector()),"documentPlugin")
    //let fns3 = kernel.ImportSemanticFunctionsFromDirectory( Environment.ExpandEnvironmentVariables("%SK_SAMPLES_HOME%"),"SummarizeSkill")
    //let m1 = fns3.["Summarize"]    
    let ctx = kernel.CreateNewContext()
    
    {Kernel=kernel; Context=ctx}

let fnDoc = @"C:\Users\Faisa\OneDrive\Documents\FsTsetlinPaper.docx"

let fn = DocumentPlugin(WordDocumentConnector(),FileSystem.LocalFileSystemConnector())
let dc1 = fn.ReadTextAsync(fnDoc) |> Async.AwaitTask |> Async.RunSynchronously

type ReadDoc = FuncProvider<"{{documentPlugin.ReadText $doc}}">
type SummSkill = FuncProvider< @"%SK_SAMPLES_HOME%",Skills="SummarizeSkill">

let rs0 = kstate() |> ReadDoc.kerlet(doc=fnDoc) |> Async.RunSynchronously
rs0.Context.Variables.Input
rs0.Context.Variables.["input"]
rs0.Context.Variables.Keys
let rs2 = rs0 |> SummSkill.SummarizeSkill.Summarize() |> Async.RunSynchronously
rs2.Context.Variables.Input

let plan =
    ReadDoc.kerlet(doc=fnDoc)
    >>= SummSkill.SummarizeSkill.Summarize()

let rs1 = kstate() |> plan |> Async.RunSynchronously
rs1.Context.Result
(*
debugging - open visual studio dev command prompt in elevate mode
cd C:\Users\Faisa\source\repos\SKProvider\tests\SKProvider.Tests\scripts 
devenv /debugexe fsi.exe test.fsx

*)
