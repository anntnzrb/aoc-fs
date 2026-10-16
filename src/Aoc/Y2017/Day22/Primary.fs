// approach: simulate turn, state transition and movement on a sparse grid, counting transitions into infection
// cost: O((n + b) log(n + b)) time and O(n + b) memory for n map cells and b bursts; three parsed-cell passes for centring, then sparse traversals
// trade: immutable maps represent an unbounded grid directly, at the cost of a tree update per burst
module Aoc.Y2017.Day22.Primary

open Aoc.Solution
open Aoc.Y2017.Day22.Input

type private State =
    | Weakened
    | Infectious
    | Flagged

let private infections evolved bursts infected =
    let initial =
        infected |> List.map (fun position -> position, Infectious) |> Map.ofList

    let _, _, _, count =
        seq { 1..bursts }
        |> Seq.fold
            (fun (nodes, (x, y), (dx, dy), count) _ ->
                let next, dx, dy, added =
                    match Map.tryFind (x, y) nodes with
                    | None ->
                        if evolved then
                            Some Weakened, dy, -dx, 0
                        else
                            Some Infectious, dy, -dx, 1
                    | Some Weakened -> Some Infectious, dx, dy, 1
                    | Some Infectious -> (if evolved then Some Flagged else None), -dy, dx, 0
                    | Some Flagged -> None, -dx, -dy, 0

                let nodes =
                    match next with
                    | None -> Map.remove (x, y) nodes
                    | Some state -> Map.add (x, y) state nodes

                nodes, (x + dx, y + dy), (dx, dy), count + added)
            (initial, (0, 0), (0, -1), 0)

    count

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

            Answers.both (infections false 10000 infected |> string) (infections true 10000000 infected |> string))
