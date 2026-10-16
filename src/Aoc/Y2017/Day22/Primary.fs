// approach: track infected coordinates on the infinite grid, turning before toggling each visited node and moving
// cost: O((n + b) log(n + b)) time and O(n + b) memory for n map cells and b bursts; one map traversal after parsing
// trade: an immutable set stores only infected nodes, so the grid needs no boundary or preallocated extent
module Aoc.Y2017.Day22.Primary

open Aoc.Solution
open Aoc.Y2017.Day22.Input

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun (InfectionMap rows) ->
            let centreY = List.length rows / 2

            let infected =
                rows
                |> List.mapi (fun y row ->
                    let centreX = List.length row / 2

                    row
                    |> List.indexed
                    |> List.choose (fun (x, node) ->
                        match node with
                        | Clean -> None
                        | Infected -> Some(x - centreX, y - centreY)))
                |> List.concat
                |> Set.ofList

            let _, _, _, infections =
                seq { 1..10000 }
                |> Seq.fold
                    (fun (nodes, (x, y), (dx, dy), count) _ ->
                        let nodes, (dx, dy), count =
                            if Set.contains (x, y) nodes then
                                Set.remove (x, y) nodes, (-dy, dx), count
                            else
                                Set.add (x, y) nodes, (dy, -dx), count + 1

                        nodes, (x + dx, y + dy), (dx, dy), count)
                    (infected, (0, 0), (0, -1), 0)

            Answers.both (string infections) "0")
