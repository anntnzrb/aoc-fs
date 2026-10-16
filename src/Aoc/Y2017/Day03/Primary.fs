// approach: walk growing spiral legs; find the target's distance and sum previously written neighbours until their value exceeds the target
// cost: O(n + k log k) time, O(k) live memory for k stress-test cells, one input pass
// trade: a lazy coordinate stream serves both rules; an immutable map stores only written cells
module Aoc.Y2017.Day03.Primary

open Aoc.Solution
open Aoc.Y2017.Day03.Input
open FsToolkit.ErrorHandling

let private positions () =
    Seq.initInfinite (fun leg ->
        let direction =
            match leg % 4 with
            | 0 -> 1, 0
            | 1 -> 0, 1
            | 2 -> -1, 0
            | _ -> 0, -1

        Seq.replicate (leg / 2 + 1) direction)
    |> Seq.concat
    |> Seq.scan (fun (x, y) (dx, dy) -> x + dx, y + dy) (0, 0)

let private firstLarger number =
    positions ()
    |> Seq.skip 1
    |> Seq.scan
        (fun (memory, _) (x, y) ->
            let value =
                [
                    for dx in -1 .. 1 do
                        for dy in -1 .. 1 do
                            if dx <> 0 || dy <> 0 then
                                yield memory |> Map.tryFind (x + dx, y + dy) |> Option.defaultValue 0L
                ]
                |> List.sum

            Map.add (x, y) value memory, value)
        (Map.ofList [ (0, 0), 1L ], 1L)
    |> Seq.map snd
    |> Seq.tryFind (fun value -> value > int64 number)

let solve: Solver =
    fun input ->
        result {
            let! (Square number) = Input.parse input
            let x, y = positions () |> Seq.item (number - 1)

            let! value =
                firstLarger number
                |> Result.requireSome "spiral ended before a larger value was written"

            return Answers.both (abs x + abs y |> string) (string value)
        }
