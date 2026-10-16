// approach: walk the spiral's alternating horizontal and vertical legs, increasing their length after every two turns
// cost: O(n) time, O(1) live memory, one input pass
// trade: a lazy coordinate stream follows the allocation rule directly without storing the grid
module Aoc.Y2017.Day03.Primary

open Aoc.Solution
open Aoc.Y2017.Day03.Input

let private positions () =
    Seq.initInfinite (fun leg ->
        let direction =
            match leg % 4 with
            | 0 -> 1, 0
            | 1 -> 0, 1
            | 2 -> -1, 0
            | _ -> 0, -1

        Seq.replicate (leg / 2 + 1) direction)
    |> Seq.concat
    |> Seq.scan (fun (x, y) (dx, dy) -> x + dx, y + dy) (0, 0)

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun (Square number) ->
            let x, y =
                positions () |> Seq.take number |> Seq.fold (fun _ position -> position) (0, 0)

            Answers.both (abs x + abs y |> string) "0")
