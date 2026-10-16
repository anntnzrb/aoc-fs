module Aoc.Cli.Program

open System
open System.Text.RegularExpressions
open Aoc
open Aoc.Solution

let private selection (args: string array) =
    let key (year: string) (day: string) variant =
        match Int32.TryParse year, Int32.TryParse day with
        | (true, year), (true, day) when Calendar.isPuzzleDay year day ->
            Some
                {
                    Year = year
                    Day = day
                    Variant = variant
                }
        | _ -> None

    match args with
    | [| year; day |] -> key year day Primary
    | [| year; day; name |] when Regex.IsMatch(name, "\\A[a-z][a-z0-9]*\\z") -> key year day (Exploration name)
    | _ -> None

let private run key input =
    match input with
    | text when String.length text = 0 -> 0
    | _ ->
        match Registry.solvers |> Map.tryFind key with
        | None ->
            let suffix =
                match key.Variant with
                | Primary -> ""
                | Exploration name -> " " + name

            eprintfn "no solver for %d/%02d%s" key.Year key.Day suffix
            1
        | Some solve ->
            match solve input with
            | Error message ->
                eprintfn "%s" message
                1
            | Ok answers ->
                answers |> Answers.render |> Console.Out.Write
                0

[<EntryPoint>]
let main args =
    try
        match selection args with
        | None ->
            Console.Error.WriteLine "Usage: aoc YEAR DAY [EXPLORATION] < input"
            2
        | Some key -> Console.In.ReadToEnd() |> run key
    with error ->
        eprintfn "%s" error.Message
        1
