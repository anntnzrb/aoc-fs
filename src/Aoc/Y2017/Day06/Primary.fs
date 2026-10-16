// approach: redistribute the first maximum bank; the first repeated configuration gives total cycles and the gap from its first visit
// cost: O(c * n * log c) time, O(c * n) memory for c cycles and n banks; three parsing passes, one working-state pass, then bank traversals per cycle
// trade: immutable configurations give structural map keys; first-visit indices recover both answers in one simulation
module Aoc.Y2017.Day06.Primary

open Aoc.Solution
open Aoc.Y2017.Day06.Input
open FsToolkit.ErrorHandling

let private redistribute (banks: int64 list) =
    let count = List.length banks

    let source, blocks =
        banks
        |> List.indexed
        |> List.fold
            (fun (source, most) (index, blocks) -> if blocks > most then index, blocks else source, most)
            (0, 0L)

    let rounds, remainder = blocks / int64 count, int (blocks % int64 count)

    let update index current =
        let distance = (index - source - 1 + count) % count
        let retained = if index = source then 0L else current
        retained + rounds + (if distance < remainder then 1L else 0L)

    banks |> List.mapi update

let private cycles banks =
    Seq.unfold
        (fun (banks, seen, cycle) ->
            let previous = Map.tryFind banks seen
            Some((cycle, previous), (redistribute banks, Map.add banks cycle seen, cycle + 1)))
        (banks, Map.empty, 0)
    // Conserved blocks and a fixed number of banks guarantee a repeated configuration.
    |> Seq.tryPick (fun (cycle, previous) -> previous |> Option.map (fun first -> cycle, cycle - first))
    |> Result.requireSome "redistribution ended before a repeated configuration"

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.bind (fun (Banks(first, rest)) ->
            first :: rest
            |> List.map int64
            |> cycles
            |> Result.map (fun (total, loop) -> Answers.both (string total) (string loop)))
