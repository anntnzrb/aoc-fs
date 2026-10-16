// approach: decode row hashes high bit first, count used squares, then remove each orthogonally connected region
// cost: O(128 * 64 * (k + 9) * 256 + u log(u + 1)) time for key length k and u used squares, O(k + 128^2) memory; two input scans, one text and 64 length traversals per row hash, three used-list traversals
// trade: dense bytes avoid hexadecimal conversion; an immutable coordinate set makes absent neighbours and disk edges identical
module Aoc.Y2017.Day14.Primary

open Aoc.Solution
open Aoc.Y2017.Day14.Input

let private usedSquares key =
    [
        for row in 0..127 do
            let hash = $"%s{key}-%d{row}" |> Aoc.KnotHash.dense

            for block in 0..15 do
                for bit in 0..7 do
                    if (int hash[block] &&& (1 <<< (7 - bit))) <> 0 then
                        yield row, block * 8 + bit
    ]

let private regions used =
    let rec clear frontier remaining =
        match frontier with
        | [] -> remaining
        | (row, col) :: rest ->
            match Set.contains (row, col) remaining with
            | false -> clear rest remaining
            | true ->
                let neighbours =
                    (row - 1, col) :: (row + 1, col) :: (row, col - 1) :: (row, col + 1) :: rest

                clear neighbours (Set.remove (row, col) remaining)

    used
    |> List.fold
        (fun (remaining, count) square ->
            match Set.contains square remaining with
            | false -> remaining, count
            | true -> clear [ square ] remaining, count + 1)
        (Set.ofList used, 0)
    |> snd

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun (Key key) ->
            let used = usedSquares key
            Answers.both (used.Length |> string) (regions used |> string))
