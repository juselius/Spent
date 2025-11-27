module Main

open System
open Serilog
open Serilog.Events
open Fargo
open Fargo.Operators

open Settings

type EpicArgs = {
        Name: string option
    }

type SpendArgs = {
        At: DateTime option
        Time: string
        Summary: string option
    }
type private Cmd =
 | EpicCmd
 | SpendCmd

type Command =
  | Epics of EpicArgs
  | Spend of SpendArgs

let configureSerilog level =
    let n =
        match level with
        | 0 -> LogEventLevel.Error
        | 1 -> LogEventLevel.Warning
        | 2 -> LogEventLevel.Information
        | 3 -> LogEventLevel.Debug
        | _ -> LogEventLevel.Verbose
    LoggerConfiguration()
        .MinimumLevel.Is(n)
        .WriteTo.Console()
        .CreateLogger()

let argParser: Arg<Command * int> =
    fargo {
        let! logLevel =
            opt "log-level" null "level" "Log level (0=Verbose, 1=Debug, 2=Information, 3=Warning, 4=Error)"
            |> optParse (fun s ->
                match Int32.TryParse s with
                | true, v when v >= 0 && v <= 4 -> Ok v
                | true, _ -> Error "Log level must be between 0 and 4"
                | false, _ -> Error "Invalid log level value"
            )
            |> defaultValue 1

        let! mainCommand =
            cmd "list" null "List epics" |>> Cmd.EpicCmd
            <|> (cmd "log" null "Log spent time" |>> Cmd.SpendCmd)
            <|> (error "Invalid or missing command")

        match mainCommand with
        | Cmd.EpicCmd ->
            let! name = opt "name" "n" "filter" "Epic match filter"
            return
                Epics {
                    Name = name
                },
                logLevel

        | Cmd.SpendCmd ->
            let! whence = opt "at" "a" "datetime" "When" |> optParse (fun a -> Ok (DateTime.Parse a))
            and! summary = opt "summary" "s" "string" "Summary" |> optParse Ok
            and! time = arg "time" "GitLab time string" |> reqArg
            return
                Spend {
                    At = whence
                    Time = time
                    Summary = summary
                },
                logLevel
    }

let executeCommand (ct: System.Threading.CancellationToken) (command: Command * int) =
    task {
        let logLevel = snd command
        Log.Logger <- configureSerilog logLevel

        match fst command with
        | Epics args ->
            return 0
        | Spend args ->
            return 0
    }

[<EntryPoint>]
let main argv =
    run "spend" argParser argv executeCommand
    0