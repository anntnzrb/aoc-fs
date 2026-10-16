// approach: redistribute the first maximum bank and count transitions until a complete bank configuration repeats
// cost: O(c * n * log c) time, O(c * n) memory for c cycles and n banks; three parsing passes, then bank traversals per cycle
// trade: immutable configurations give structural set keys; quotient and remainder replace individual block moves
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
        (fun (banks, seen) ->
            if Set.contains banks seen then
                None
            else
                Some((), (redistribute banks, Set.add banks seen)))
        (banks, Set.empty)
    |> Seq.length

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun banks -> Answers.both (cycles banks |> string) "0")
