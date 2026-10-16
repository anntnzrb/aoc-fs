// approach: add each hex move in north/northeast coordinates and compute the shortest displacement
// cost: O(n) time, O(n) memory, one parsing pass and one traversal of the parsed path
// trade: two coordinates replace a grid search; their implicit third coordinate gives the distance
module Aoc.Y2017.Day11.Primary

open Aoc.Solution
open Aoc.Y2017.Day11.Input

let private move (q, r) =
    function
    | North -> q, r + 1
    | Northeast -> q + 1, r
    | Southeast -> q + 1, r - 1
    | South -> q, r - 1
    | Southwest -> q - 1, r
    | Northwest -> q - 1, r + 1

let private distance (q, r) =
    // Each step changes two of q, r, -q-r by one, so the largest magnitude is a lower bound and is reachable.
    max (abs q) (max (abs r) (abs (q + r)))

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun path ->
            let final = path |> List.fold move (0, 0)
            Answers.both (distance final |> string) "0")
