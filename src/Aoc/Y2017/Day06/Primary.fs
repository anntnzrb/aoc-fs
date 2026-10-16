// approach: redistribute the first maximum bank; the first repeated configuration gives total cycles and the gap from its first visit
// cost: O(c * n * log c) time, O(c * n) memory for c cycles and n banks; three parsing passes, then bank traversals per cycle
// trade: immutable configurations give structural map keys; first-visit indices recover both answers in one simulation
module Aoc.Y2017.Day06.Primary

open Aoc.Solution
open Aoc.Y2017.Day06.Input

let private redistribute (Banks(first, rest)) =
    let count = 1 + List.length rest

    let source, blocks =
        rest
        |> List.indexed
        |> List.fold
            (fun (source, most) (index, blocks) -> if blocks > most then index + 1, blocks else source, most)
            (0, first)

    let rounds, remainder = blocks / count, blocks % count

    let update index current =
        let distance = (index - source - 1 + count) % count
        let retained = if index = source then 0 else current
        retained + rounds + (if distance < remainder then 1 else 0)

    Banks(update 0 first, rest |> List.mapi (fun index blocks -> update (index + 1) blocks))

let private cycles banks =
    Seq.unfold
        (fun (banks, seen, cycle) ->
            let previous = Map.tryFind banks seen
            Some((cycle, previous), (redistribute banks, Map.add banks cycle seen, cycle + 1)))
        (banks, Map.empty, 0)
    // Conserved blocks and a fixed number of banks guarantee a repeated configuration.
    |> Seq.pick (fun (cycle, previous) -> previous |> Option.map (fun first -> cycle, cycle - first))

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun banks ->
            let total, loop = cycles banks
            Answers.both (string total) (string loop))
