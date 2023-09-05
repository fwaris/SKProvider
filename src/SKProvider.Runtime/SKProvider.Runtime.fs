namespace SKProvider
open System
open Microsoft.SemanticKernel

module TemplateParser =
    [<AutoOpen>]
    module internal StateMachine =
        let eof = Seq.toArray "<end of input>" 
        let inline error x xs = failwithf "%s got %s" x (String(xs |> Seq.truncate 100 |> Seq.toArray))
        
        let rec start acc = function
            | [] -> acc
            | '{'::rest -> brace1 acc rest 
            | _::rest -> start acc rest
        and brace1 acc = function
            | [] -> error "expected {" eof
            | '{'::rest -> brace2 acc rest
            | x::rest   -> error "expected {" rest
        and brace2 acc = function
            | [] -> error "expecting $ after {{" eof
            | '$'::rest -> beginVar [] acc rest
            | c::rest when c <> '}' && c <> '{' -> brace2 acc rest 
            | xs -> error "Expected '$'" xs
        and beginVar vacc acc = function
            | [] -> error "expecting }" eof
            | '}'::rest -> braceEnd1 (vacc::acc) rest
            | c::rest when (Char.IsWhiteSpace c) -> braceEnd1 (vacc::acc) rest
            | x::rest -> beginVar (x::vacc) acc rest
        and braceEnd1 acc = function
            | [] -> error "expecting }" eof
            | '}'::rest -> start acc rest
            | ' '::rest -> braceEnd1 acc rest //can ignore whitespace
            | xs        -> error "expecting }}" xs

    let extractVars templateStr = 
        start [] (templateStr |> Seq.toList) 
        |> List.map(fun xs -> String(xs |> Seq.rev |> Seq.toArray)) 
        |> List.distinct


// Put any utilities here
[<AutoOpen>]
module internal Utilities = 
    ()

type KState = {Kernel:IKernel; Context:Orchestration.SKContext}
type Kerlet = KState -> Async<KState>

module Ops =
    let (>>=) (a:Kerlet) (b:Kerlet) :Kerlet = 
        fun ctx -> 
            async{
                let! ctx' = a ctx
                return! (b ctx')
            }

// Put the TypeProviderAssemblyAttribute in the runtime DLL, pointing to the design-time DLL
[<assembly:CompilerServices.TypeProviderAssembly("SKProvider.DesignTime.dll")>]
do ()
