namespace SKProvider
open System
open Microsoft.SemanticKernel

// Put any utilities here
[<AutoOpen>]
module internal Utilities = 
    type DummyType = class end 
    ()

// Put the TypeProviderAssemblyAttribute in the runtime DLL, pointing to the design-time DLL
[<assembly:CompilerServices.TypeProviderAssembly("SKProvider.DesignTime.dll")>]
do ()
