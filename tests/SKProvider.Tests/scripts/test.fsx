#r "nuget: Microsoft.SemanticKernel, 1.0.0-beta1"
#r "nuget: SKProvider.Runtime"
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
