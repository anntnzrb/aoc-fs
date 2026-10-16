// approach: interpret the blueprint over a zipper of 64-bit tape words, shifting the focus within its current word
// cost: O(n + s log q + v/64) time, O(n + q + v/64) live memory; one text pass
// trade: word packing preserves immutability with allocation only at word crossings; pinned 12M-example Release medians: primary 597 ms versus old hash-set 1159 ms (3 runs)
module Aoc.Y2017.Day25.Primary

open System.Numerics
open Aoc.Solution
open Aoc.Y2017.Day25.Input

let rec private run states remaining state (left: uint64 list) (current: uint64) offset (right: uint64 list) =
    if remaining = 0 then
        Ok(
            List.sumBy (fun (word: uint64) -> BitOperations.PopCount word) left
            + BitOperations.PopCount current
            + List.sumBy (fun (word: uint64) -> BitOperations.PopCount word) right
        )
    else
        match Map.tryFind state states with
        | None -> Error "undefined machine state"
        | Some rules ->
            let action =
                if (current >>> offset) &&& 1UL = 0UL then
                    rules.IfZero
                else
                    rules.IfOne

            let mask = 1UL <<< offset

            let written =
                if action.Write = One then
                    current ||| mask
                else
                    current &&& ~~~mask

            match action.Move with
            | Left when offset > 0 -> run states (remaining - 1) action.Next left written (offset - 1) right
            | Right when offset < 63 -> run states (remaining - 1) action.Next left written (offset + 1) right
            | Left ->
                match left with
                | [] -> run states (remaining - 1) action.Next [] 0UL 63 (written :: right)
                | head :: tail -> run states (remaining - 1) action.Next tail head 63 (written :: right)
            | Right ->
                match right with
                | [] -> run states (remaining - 1) action.Next (written :: left) 0UL 0 []
                | head :: tail -> run states (remaining - 1) action.Next (written :: left) head 0 tail

let solve: Solver =
    fun input ->
        Input.parse input
        |> Result.bind (fun blueprint -> run blueprint.States blueprint.Steps blueprint.Start [] 0UL 0 [])
        |> Result.map (string >> Answers.part1)
