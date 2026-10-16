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
    let lines = input.Replace("\r\n", "\n").TrimEnd('\n').Split('\n')
    let width = lines[0].Length

    if
        width % 2 = 0
        || lines.Length % 2 = 0
        || lines |> Array.exists (fun line -> line.Length <> width)
    then
        Error "expected an odd-sized rectangular map"
    else
        lines
        |> Array.fold
            (fun rows line ->
                rows
                |> Result.bind (fun rows ->
                    line
                    |> Seq.fold
                        (fun nodes cell ->
                            nodes
                            |> Result.bind (fun nodes ->
                                match cell with
                                | '.' -> Ok(Clean :: nodes)
                                | '#' -> Ok(Infected :: nodes)
                                | _ -> Error "expected a clean or infected node"))
                        (Ok [])
                    |> Result.map (fun nodes -> List.rev nodes :: rows)))
            (Ok [])
        |> Result.map (List.rev >> InfectionMap)
