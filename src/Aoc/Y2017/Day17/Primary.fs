// approach: insert each new value after the circularly advanced position, then read the last insertion's successor
// cost: O(n²) time and O(n) memory for n insertions; one input pass
// trade: an immutable list directly models insertion; the small buffer needs no specialised structure
module Aoc.Y2017.Day17.Primary

open Aoc.Solution
open Aoc.Y2017.Day17.Input

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.bind (fun (Steps steps) ->
            let position, buffer =
                [ 1..2017 ]
                |> List.fold
                    (fun (position, buffer) value ->
                        let next = (position + steps % value) % value + 1
                        next, buffer |> List.insertAt next value)
                    (0, [ 0 ])

            match buffer |> List.tryItem ((position + 1) % 2018) with
            | Some value -> Ok(Answers.both (string value) "0")
            | None -> Error "missing successor")
