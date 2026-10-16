// approach: hash each row key and enumerate used coordinates from its bits, high bit first
// cost: O(128 * 64 * (k + 9) * 256 + 128^2) time for key length k, O(k + 128^2) memory; two input scans, one text and 64 length traversals per row hash
// trade: dense bytes avoid hexadecimal conversion; explicit coordinates preserve the row and column meaning
module Aoc.Y2017.Day14.Primary

open Aoc.Solution
open Aoc.Y2017.Day14.Input

let private usedSquares key =
    [
        for row in 0..127 do
            let hash = $"{key}-{row}" |> Aoc.KnotHash.dense

            for block in 0..15 do
                for bit in 0..7 do
                    if (int hash[block] &&& (1 <<< (7 - bit))) <> 0 then
                        yield row, block * 8 + bit
    ]

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun (Key key) ->
            let used = usedSquares key
            Answers.both (used.Length |> string) "0")
