/// The finite, centred map of initially clean or infected nodes.
module Aoc.Y2017.Day22.Input

/// A node's initial infection status.
type Node =
    | Clean
    | Infected

/// Rows in top-to-bottom order, each with the same odd width.
type InfectionMap = InfectionMap of Node list list

/// Parses a non-empty rectangular map with a unique centre node.
let parse (input: string) : Result<InfectionMap, string> =
    let lines = input.Replace("\r\n", "\n").TrimEnd('\n').Split('\n') |> Array.toList

    match lines with
    | first :: _ when
        first.Length % 2 <> 0
        && lines.Length % 2 <> 0
        && lines |> List.forall (fun line -> line.Length = first.Length)
        ->
        lines
        |> Aoc.Parse.traverse (
            Aoc.Parse.traverse (function
                | '.' -> Ok Clean
                | '#' -> Ok Infected
                | _ -> Error "expected a clean or infected node")
        )
        |> Result.map InfectionMap
    | _ -> Error "expected an odd-sized rectangular map"
