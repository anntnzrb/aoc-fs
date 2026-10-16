// approach: a scanner returns to the top after an outward and return trip; sum severity when arrival coincides with that period
// cost: O(n) time and memory, one parsing pass and one traversal of the parsed layers
// trade: modular timing replaces a simulation of every scanner position
module Aoc.Y2017.Day13.Primary

open Aoc.Solution
open Aoc.Y2017.Day13.Input

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun layers ->
            let severity =
                layers
                |> List.sumBy
                    (fun
                        {
                            Depth = Depth depth
                            Range = Range range
                        } ->
                        let depth, range = int64 depth, int64 range

                        if range = 1L || depth % (2L * (range - 1L)) = 0L then
                            depth * range
                        else
                            0L)

            Answers.both (string severity) "0")
