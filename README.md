# SKProvider

Enables typed representations of Semantic Kernel prompts. E.g.:

```F#
open SKProvider 

[<Literal>]
let Template1 = """ 
Summarize:
{{$input}}

Today is {{timeSkill.Now}}

Using:
{{$context}}
"""

type T1 = FuncProvider<Template1> //typed prompt template
...
let kr = T1.kerlet(context="this is a context", input="this is the input") //bound template
...

```

Building:

    dotnet tool restore
    dotnet paket update
    dotnet build -c release

    dotnet pack SKProvider.sln -o nuget -c Release -p:Version=1.0.0.1

During development, may need to clear nuget cache often (especially when working with scripts):

    dotnet fsi .\tests\SKProvider.Tests\scripts\ClearNugetCache.fsx
