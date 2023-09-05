module SKProviderTests
open Microsoft.SemanticKernel
open SKProvider
open NUnit.Framework

[<Literal>]
let Template1 = """
Summarize:
{{$input}}

Using:
{{$context}}
"""

type T1 = FuncProvider<Template1>

[<Test>]
let testBase() = 
    let kernel = Kernel.Builder.Build()
    let ctx = kernel.CreateNewContext()
    let ks = {Kernel=kernel; Context=ctx}
    let ks' = T1.kerlet(context="this is a context", input = "this is the input") ks |> Async.RunSynchronously
    Assert.IsTrue(true)

