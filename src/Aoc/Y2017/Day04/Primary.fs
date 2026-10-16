// approach: count unique words for part 1 and unique sorted-letter keys for part 2, since rearrangements have identical sorted letters
// cost: O(n (log w + log l)) time, O(n) memory; normalization/splitting, validation, then one parsed-phrase traversal; n is input length, w max words per phrase, l max word length
// trade: immutable sets share the uniqueness rule; sorting keys allocates letter lists but avoids enumerating rearrangements
module Aoc.Y2017.Day04.Primary

open Aoc.Solution
open Aoc.Y2017.Day04.Input

let private uniqueBy key words =
    words |> List.map key |> Set.ofList |> Set.count = List.length words

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun phrases ->
            let part1, part2 =
                phrases
                |> List.fold
                    (fun (part1, part2) (Passphrase words) ->
                        let exact = if uniqueBy id words then 1 else 0
                        let anagrams = if uniqueBy (Seq.sort >> Seq.toList) words then 1 else 0
                        part1 + exact, part2 + anagrams)
                    (0, 0)

            Answers.both (string part1) (string part2))
