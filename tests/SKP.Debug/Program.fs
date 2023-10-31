open Microsoft.SemanticKernel
open SKProvider
open SKProvider.Ops

let kstate() = 
    let kernel = Kernel.Builder.Build()
    let ctx = kernel.CreateNewContext()
    {Kernel=kernel; Context=ctx}

[<Literal>]
let Template1 = """
Summarize:
{{$input}}

Today is {{timeSkill.Now}}

Using:
{{$context}}
"""

type T1 = FuncProvider<Template1>

type T2 = FuncProvider< @"E:\s\repos\semantic-kernel\samples\skills",Skills="QASkill">

let k1 = T1.kerlet(context="Some context", input="some input") 
let ks = kstate()
let rs1 = k1 ks |> Async.RunSynchronously

(*
debugging - open visual studio dev command prompt in elevate mode
cd C:\Users\Faisa\source\repos\SKProvider\tests\SKProvider.Tests\scripts 
devenv /debugexe fsi.exe test.fsx

*)