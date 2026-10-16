// approach: simulate the small buffer; for the large run track only insertions immediately after zero
// cost: O(n² + m) time, O(n) memory for n small and m large insertions; one input pass
// trade: the small list models insertion directly; the large run discards values that cannot affect zero's successor
module Aoc.Y2017.Day17.Primary

open Aoc.Solution
open Aoc.Y2017.Day17.Input

let private afterZero steps =
    let mutable position = 0
    let mutable successor = 0

    for value in 1..50_000_000 do
        position <- (position + steps % value) % value + 1
        // Insertion never precedes index zero, so only index one replaces its successor.
        if position = 1 then
            successor <- value

    successor

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
            | Some value -> Ok(Answers.both (string value) (afterZero steps |> string))
            | None -> Error "missing successor")
