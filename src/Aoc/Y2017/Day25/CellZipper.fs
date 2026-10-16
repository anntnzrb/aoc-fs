// approach: interpret the blueprint over a cell zipper, with unvisited tape cells defaulting to zero
// cost: O(n + s log q + v) time, O(n + q + v) live memory for s steps, q states and v visited cells; one text pass
// trade: the textbook tape is simpler but allocates one list node per move; pinned 12M-example Release medians: 1932 ms versus word primary 597 ms (3 runs)
module Aoc.Y2017.Day25.CellZipper

open Aoc.Solution
open Aoc.Y2017.Day25.Input

[<TailCall>]
let rec private run states remaining state left current right =
    if remaining = 0 then
        let count = List.sumBy (fun bit -> if bit then 1 else 0)
        Ok(count left + (if current then 1 else 0) + count right)
    else
        match Map.tryFind state states with
        | None -> Error "undefined machine state"
        | Some rules ->
            let action = if current then rules.IfOne else rules.IfZero
            let written = action.Write = One

            match action.Move, left, right with
            | Left, [], _ -> run states (remaining - 1) action.Next [] false (written :: right)
            | Left, head :: tail, _ -> run states (remaining - 1) action.Next tail head (written :: right)
            | Right, _, [] -> run states (remaining - 1) action.Next (written :: left) false []
            | Right, _, head :: tail -> run states (remaining - 1) action.Next (written :: left) head tail

let solve: Solver =
    fun input ->
        Input.parse input
        |> Result.bind (fun blueprint -> run blueprint.States blueprint.Steps blueprint.Start [] false [])
        |> Result.map (string >> Answers.part1)
