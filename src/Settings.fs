module Settings

open System.IO
open Thoth.Json.Net
open Serilog

type Settings = { GitLab: string }

let tryGetEnv =
    System.Environment.GetEnvironmentVariable
    >> function
        | null
        | "" -> None
        | x -> Some x