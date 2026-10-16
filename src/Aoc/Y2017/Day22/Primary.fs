// approach: simulate turn, state transition and movement on a sparse grid, counting transitions into infection
// cost: expected O(n + b) time and O(n + b) memory for n map cells and b bursts; two text passes, one cell parse and three parsed-cell passes for centring
// trade: a local dictionary keeps the grid unbounded without persistent-tree updates; mutation stays inside each independent simulation
module Aoc.Y2017.Day22.Primary

open System.Collections.Generic
open Aoc.Solution
open Aoc.Y2017.Day22.Input

type private State =
    | Weakened
    | Infected
    | Flagged

type private Mode =
    | Basic
    | Evolved

let private infections mode bursts infected =
    let nodes = Dictionary<struct (int * int), State>()

    for position in infected do
        nodes[position] <- Infected

    let mutable x = 0
    let mutable y = 0
    let mutable dx = 0
    let mutable dy = -1
    let mutable count = 0

    for _ in 1..bursts do
        let position = struct (x, y)

        let next, nextDx, nextDy, added =
            match mode, nodes.TryGetValue position with
            | Basic, (false, _) -> Some Infected, dy, -dx, 1
            | Evolved, (false, _) -> Some Weakened, dy, -dx, 0
            | _, (true, Weakened) -> Some Infected, dx, dy, 1
            | Basic, (true, Infected) -> None, -dy, dx, 0
            | Evolved, (true, Infected) -> Some Flagged, -dy, dx, 0
            | _, (true, Flagged) -> None, -dx, -dy, 0

        match next with
        | None -> nodes.Remove position |> ignore
        | Some state -> nodes[position] <- state

        dx <- nextDx
        dy <- nextDy
        x <- x + dx
        y <- y + dy
        count <- count + added

    count

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun (InfectionMap rows) ->
            let centreY = List.length rows / 2

            let infected =
                [
                    for y, row in List.indexed rows do
                        let centreX = List.length row / 2

                        for x, node in List.indexed row do
                            match node with
                            | Clean -> ()
                            | Input.Infected -> yield struct (x - centreX, y - centreY)
                ]

            Answers.both (infections Basic 10000 infected |> string) (infections Evolved 10000000 infected |> string))
