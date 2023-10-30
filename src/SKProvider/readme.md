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
