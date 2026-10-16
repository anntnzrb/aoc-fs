// approach: enumerate bridges from exposed port zero, keeping each component's identity and the strongest prefix
// cost: O(n B log n) time for B bridge prefixes and n components, O(n^2 log n) live DFS storage; one text parse and one preparation pass, then a piece scan per prefix
// trade: an explicit depth-first frontier keeps search tail recursive; immutable used-id sets preserve duplicate pieces
module Aoc.Y2017.Day24.Primary

open Aoc.Solution
open Aoc.Y2017.Day24.Input

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun components ->
            let pieces = components |> List.mapi (fun id (Component(a, b)) -> id, a, b)

            let rec search best =
                function
                | [] -> best
                | (port, strength, used) :: rest ->
                    let frontier =
                        pieces
                        |> List.fold
                            (fun pending (id, a, b) ->
                                if not (Set.contains id used) && (a = port || b = port) then
                                    let next = if a = port then b else a
                                    (next, strength + a + b, Set.add id used) :: pending
                                else
                                    pending)
                            rest

                    search (max best strength) frontier

            Answers.both (search 0 [ 0, 0, Set.empty ] |> string) "0")
