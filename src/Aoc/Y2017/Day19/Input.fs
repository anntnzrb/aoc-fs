/// Domain model and parser for the routing diagram.
module Aoc.Y2017.Day19.Input

/// Diagram rows retain spaces because their columns determine connectivity.
type Diagram = { Rows: string list; Entrance: int }

/// Parses diagram characters and the unique entrance on its top row.
let parse (input: string) : Result<Diagram, string> =
    let rows = input.Replace("\r\n", "\n").TrimEnd('\n').Split('\n') |> Array.toList

    let valid c =
        c = ' ' || c = '|' || c = '-' || c = '+' || System.Char.IsAsciiLetterUpper c

    match rows |> List.tryHead with
    | Some top when rows |> List.forall (Seq.forall valid) ->
        match
            top
            |> Seq.indexed
            |> Seq.choose (fun (x, c) -> if c = '|' then Some x else None)
            |> Seq.toList
        with
        | [ entrance ] -> Ok { Rows = rows; Entrance = entrance }
        | _ -> Error "expected one entrance on the top row"
    | _ -> Error "invalid routing diagram"
