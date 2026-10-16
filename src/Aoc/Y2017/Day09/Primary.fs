// approach: parse nested groups and canceled garbage, then sum each group's depth
// cost: O(n) time and memory, one input pass and one traversal of the parsed tree
// trade: a domain tree separates the grammar from scoring; an explicit frontier avoids recursive scoring
module Aoc.Y2017.Day09.Primary

open Aoc.Solution
open Aoc.Y2017.Day09.Input

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun root ->
            let score =
                [ root, 1 ]
                |> Seq.unfold (function
                    | [] -> None
                    | (Garbage _, _) :: rest -> Some(0, rest)
                    | (Group children, depth) :: rest ->
                        Some(depth, (children |> List.map (fun child -> child, depth + 1)) @ rest))
                |> Seq.sum

            Answers.both (string score) "0")
