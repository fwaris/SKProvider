open System
open System.IO
let prof = System.Environment.GetEnvironmentVariable("USERPROFILE")
let packages = $@"{prof}\.nuget\packages"
let p1 = $@"{packages}\SKProvider.Core"
let p2 = $@"{packages}\SKProvider"
if Directory.Exists p1 then Directory.Delete(p1,true) |> ignore
if Directory.Exists p2 then Directory.Delete(p2,true) |> ignore
printfn "cache cleared"


