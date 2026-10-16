// approach: swap circular spans for one numeric round and 64 ASCII rounds, then XOR blocks and encode hex
// cost: O(n + sum of numeric lengths + 64 * sum of ASCII lengths) time, O(n + 256) memory; up to five text scans, two numeric and 64 suffixed ASCII-length traversals
// trade: local array mutation keeps reversals direct; part 1 is 0 for text without a numeric interpretation
module Aoc.Y2017.Day10.Primary

open Aoc.Solution
open Aoc.Y2017.Day10.Input

let private twist rounds lengths =
    let marks = Array.init 256 id

    Seq.replicate rounds lengths
    |> Seq.concat
    |> Seq.fold
        (fun (position, skip) length ->
            for offset in 0 .. length / 2 - 1 do
                let left = (position + offset) % 256
                let right = (position + length - 1 - offset) % 256
                let saved = marks[left]
                marks[left] <- marks[right]
                marks[right] <- saved

            // Only the skip's residue affects circular positions, including across rounds.
            (position + length + skip) % 256, (skip + 1) % 256)
        (0, 0)
    |> ignore

    marks

let private product lengths =
    let marks = lengths |> List.map (fun (Length length) -> length) |> twist 1
    marks[0] * marks[1]

let private hash text =
    let lengths = (text |> Seq.map int |> Seq.toList) @ [ 17; 31; 73; 47; 23 ]

    lengths
    |> twist 64
    |> Array.chunkBySize 16
    |> Array.map (
        Array.fold (^^^) 0
        >> fun value -> value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)
    )
    |> String.concat ""

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun puzzle ->
            let part1 = puzzle.Lengths |> Option.map product |> Option.defaultValue 0
            Answers.both (string part1) (hash puzzle.Text))
