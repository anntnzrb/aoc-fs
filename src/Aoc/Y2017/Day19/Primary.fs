// approach: walk from the top entrance, preserving direction at crossings, then derive letters and step count from the visited cells
// cost: O(n + s) time and memory for n input characters and s visited cells; line splitting and character validation passes, then one path walk and one letter-filter pass
// trade: indexed rows preserve diagram geometry without building a graph; materialising the path simplifies both answers at the cost of storing every visited cell
module Aoc.Y2017.Day19.Primary

open Aoc.Solution
open Aoc.Y2017.Day19.Input

let private walk diagram =
    let rows = diagram.Rows |> List.toArray

    let cell (x, y) =
        match rows |> Array.tryItem y with
        | Some row when x >= 0 && x < row.Length -> row[x]
        | _ -> ' '

    let advance (x, y) (dx, dy) = x + dx, y + dy

    Seq.unfold
        (fun (position, (dx, dy)) ->
            match cell position with
            | ' ' -> None
            | c ->
                let heading =
                    match c with
                    | '+' ->
                        [ -dy, dx; dy, -dx ]
                        |> List.tryFind (fun direction -> cell (advance position direction) <> ' ')
                        |> Option.defaultValue (dx, dy)
                    | _ -> dx, dy

                Some(c, (advance position heading, heading)))
        ((diagram.Entrance, 0), (0, 1))
    |> Seq.toArray

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun diagram ->
            let path = walk diagram
            let letters = path |> Array.filter System.Char.IsAsciiLetterUpper |> System.String
            Answers.both letters (string path.Length))
