// approach: walk from the top entrance, preserving direction at crossings and collecting letters in encounter order
// cost: O(n + s) time, O(n + l) memory for n input characters, s visited cells and l letters; line splitting and character validation passes, then one path traversal
// trade: indexed rows preserve diagram geometry without building a graph; a lazy unfold carries only position and heading
module Aoc.Y2017.Day19.Primary

open Aoc.Solution
open Aoc.Y2017.Day19.Input

let private letters diagram =
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
    |> Seq.filter System.Char.IsAsciiLetterUpper
    |> Seq.toArray
    |> System.String

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun diagram -> Answers.both (letters diagram) "0")
