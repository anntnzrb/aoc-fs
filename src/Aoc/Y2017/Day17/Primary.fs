// approach: simulate the small buffer; for the large run track only insertions immediately after zero
// cost: O(n² + m) time, O(n) memory for n small and m large insertions; one input pass
// trade: the small list models insertion directly; the large run discards values that cannot affect zero's successor
module Aoc.Y2017.Day17.Primary

open Aoc.Solution
open Aoc.Y2017.Day17.Input
open FsToolkit.ErrorHandling

let private advance steps position value = (position + steps % value) % value + 1

let private afterZero steps =
    let rec loop value position successor =
        if value > 50_000_000 then
            successor
        else
            let next = advance steps position value
            // Insertion never precedes index zero, so only index one replaces its successor.
            loop (value + 1) next (if next = 1 then value else successor)

    loop 1 0 0

let solve: Solver =
    fun input ->
        result {
            let! (Steps steps) = Input.parse input

            let position, buffer =
                [ 1..2017 ]
                |> List.fold
                    (fun (position, buffer) value ->
                        let next = advance steps position value
                        next, buffer |> List.insertAt next value)
                    (0, [ 0 ])

            let! value =
                buffer
                |> List.tryItem ((position + 1) % List.length buffer)
                |> Result.requireSome "missing successor"

            return Answers.both (string value) (afterZero steps |> string)
        }
