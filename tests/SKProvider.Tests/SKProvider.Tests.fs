module SKProviderTests
open Microsoft.SemanticKernel
open SKProvider
open SKProvider.Ops
open NUnit.Framework

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

type T2 = FuncProvider< @"%SK_SAMPLES_HOME%",Skills="QASkill">

[<Test>]
let testBase() = 
    let ks = kstate()
    let ks' = T1.kerlet(context="this is a context", input = "this is the input") ks |> Async.RunSynchronously
    Assert.IsTrue(true)

[<Test>]
let testFolder() = 
    let ks = kstate()
    let ks' = T2.QASkill.Form(input="this is me", promptName="mega") ks |> Async.RunSynchronously
    let ks'' = T2.QASkill.QNA() ks' |> Async.RunSynchronously
    Assert.IsTrue(true)

[<Test>]
let testBind() = 
    let ks = kstate()
    let f = T2.QASkill.Form() >>= T2.QASkill.QNA()
    let ks' = f ks |> Async.RunSynchronously
    Assert.IsTrue(true)

    