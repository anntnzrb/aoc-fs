// approach: count passphrases whose set of words has the same size as their word list
// cost: O(n log w) time, O(n) parsed memory plus O(w) set entries; one parsing pass and one pass over the phrases
// trade: immutable sets express uniqueness directly without pairwise word comparisons; n is input length, w is the largest word count
module Aoc.Y2017.Day04.Primary

open Aoc.Solution
open Aoc.Y2017.Day04.Input

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun phrases ->
            let valid =
                phrases
                |> List.sumBy (fun (Passphrase words) ->
                    if words |> Set.ofList |> Set.count = List.length words then
                        1
                    else
                        0)

            Answers.both (string valid) "0")
