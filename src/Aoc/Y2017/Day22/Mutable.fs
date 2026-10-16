// approach: simulate turn, state transition and movement on a sparse grid, counting transitions into infection
// cost: expected O(n + b) time and O(n + b) memory for n map cells and b bursts; two text passes, one cell parse and three parsed-cell passes for centring
// trade: dictionary mutation models each cell directly; pinned 10M-example Release medians: 1091 ms versus tiled DU primary 447 ms (3 runs, noisy)
module Aoc.Y2017.Day22.Mutable

open System.Collections.Generic
open Aoc.Solution
open Aoc.Y2017.Day22.Input

[<Struct>]
type private State =
    | Weakened
    | Infected
    | Flagged

[<Struct>]
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
        | None -> nodes.Remove position |> ignore<bool>
        | Some state -> nodes[position] <- state

        dx <- nextDx
        dy <- nextDy
        x <- x + dx
        y <- y + dy
        count <- count + added

    count

let internal solveWith basic evolved : Solver =
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

            Answers.both (infections Basic basic infected |> string) (infections Evolved evolved infected |> string))

let solve: Solver = solveWith 10000 10000000
