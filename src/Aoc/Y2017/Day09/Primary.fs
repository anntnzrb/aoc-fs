// approach: parse nested groups and canceled garbage, then sum group depths and remaining garbage lengths
// cost: O(n) time and memory, one input pass and one traversal of the parsed tree
// trade: a domain tree separates the grammar from both totals; an explicit frontier avoids recursive scoring
module Aoc.Y2017.Day09.Primary

open Aoc.Solution
open Aoc.Y2017.Day09.Input

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun root ->
            let score, garbage =
                [ root, 1 ]
                |> Seq.unfold (function
                    | [] -> None
                    | (Garbage text, _) :: rest -> Some((0, text.Length), rest)
                    | (Group children, depth) :: rest ->
                        Some((depth, 0), (children |> List.map (fun child -> child, depth + 1)) @ rest))
                |> Seq.fold (fun (score, garbage) (depth, count) -> score + depth, garbage + count) (0, 0)

            Answers.both (string score) (string garbage))
