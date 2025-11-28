module Main

open System
open Serilog
open Serilog.Events
open Fargo
open Fargo.Operators
open GitLab.Data.GraphQLClient

open Settings

type ListArgs = { title: string option }

type SpentArgs = {
    gid: string
    time: string
    date: string
    summary: string
}

[<RequireQualifiedAccess>]
type private Cmd =
    | List
    | Spent

type Command =
    | List of ListArgs
    | Spent of SpentArgs

let configureSerilog n =
    LoggerConfiguration().MinimumLevel.Is(n).WriteTo.Console().CreateLogger ()

let argParser: Arg<Command> =
    fargo {
        let! mainCommand =
            cmd "list" "ls" "List epics" |>> Cmd.List
            <|> (cmd "time" null "Log spent time" |>> Cmd.Spent)
            <|> error "Invalid or missing command"

        match mainCommand with
        | Cmd.List ->
            let! title = opt "title" "t" "filter" "Title filter" |> optMap _.ToLower()
            return List { title = title }
        | Cmd.Spent ->
            let! date =
                opt "date" "t" "datetime" "When"
                |> optParse (fun a -> Ok (DateTime.Parse a |> string))
                |> defaultValue (DateTime.UtcNow.AddMinutes(-1.0).ToString "u")
            and! summary = opt "summary" "s" "string" "Summary" |> defaultValue ""
            and! gid = opt "id" "i" "int or alias" "Epic id"
            and! time = arg "time" "GitLab time string" |> reqArg
            return
                Spent {
                    gid = gid |> Option.defaultValue settings.epicId
                    time = time
                    date = date
                    summary = summary
                }
    }

let gql = GraphqlClient settings.gitlabApi

let getWorkItems (group: string) =
    let input: GetWorkItems.InputVariables = { group = group }
    match gql.GetWorkItems input with
    | Ok epics ->
        epics.group.Value.workItems.Value.nodes.Value
        |> List.filter (fun y -> y.Value.state.IsOpen)
        |> List.map (fun y -> int y.Value.iid, y.Value)
        |> Map.ofList
    | Error err ->
        Log.Error $"%A{err}"
        exit 1

let itemToIssuableId (workItems: Map<int, GetWorkItems.WorkItem>) (item: string) =
    try
        let iid = int item
        workItems[iid].id
    with _ ->
        let iid = settings.aliases.TryFind item
        match iid with
        | Some n -> workItems[n].id
        | None ->
            Log.Error $"Epic id not found"
            exit 1

let executeCommand (_: Threading.CancellationToken) (command: Command) =
    task {
        Log.Logger <- configureSerilog LogEventLevel.Warning

        let workItems = getWorkItems settings.group
        match command with
        | List args ->
            let workItems =
                match args.title with
                | Some pat -> workItems |> Map.filter (fun _ v -> v.title.ToLower().Contains pat)
                | None -> workItems
            workItems |> Map.iter (fun _ v -> printfn $"%5s{v.iid}: {v.title}")
            return 0
        | Spent args ->
            let input: CreateTimelog.InputVariables = {
                issuableId = itemToIssuableId workItems args.gid
                timeSpent = args.time
                spentAt = args.date
                summary = args.summary
            }
            match gql.CreateTimelog input with
            | Ok epics ->
                if epics.timelogCreate.Value.errors.Length > 0 then
                    epics.timelogCreate.Value.errors |> Seq.iter (fun err -> Log.Warning $"{err}")
                let tl = epics.timelogCreate.Value.timelog.Value
                let spent =
                    let ts = TimeSpan.FromSeconds (int64 tl.timeSpent)
                    $"{ts.Hours}h{ts.Minutes}m"
                let summary =
                    tl.summary |> Option.map (fun s -> $"on \"{s}\"") |> Option.defaultValue ""
                printfn $"{tl.user.username} spent {spent} {summary}"
            | Error err -> Log.Error $"{err}"
            return 0
    }

[<EntryPoint>]
let main argv =
    run "spent" argParser argv executeCommand