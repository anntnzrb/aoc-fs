// approach: TODO
// cost: TODO
// trade: TODO
module Aoc.Y2017.Day24.Primary

open Aoc.Solution
open Aoc.Y2017.Day24.Input

let solve: Solver =
    fun input -> input |> Input.parse |> Result.map (fun _ -> Answers.both "0" "0")
