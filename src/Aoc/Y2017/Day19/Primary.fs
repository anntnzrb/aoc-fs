// approach: walk from the top entrance, preserving direction at crossings and collecting letters and counting every visited cell
// cost: O(n + s) time, O(n + l) memory for n input characters, s visited cells and l letters; line splitting and character validation passes, then one path traversal
// trade: indexed rows preserve diagram geometry without building a graph; a lazy unfold carries only position and heading
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
    |> Seq.fold
        (fun (letters, steps) c ->
            let letters =
                if System.Char.IsAsciiLetterUpper c then
                    c :: letters
                else
                    letters

            letters, steps + 1)
        ([], 0)
    |> fun (letters, steps) -> (letters |> List.rev |> List.toArray |> System.String), steps

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun diagram ->
            let letters, steps = walk diagram
            Answers.both letters (string steps))
