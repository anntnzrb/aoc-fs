// approach: simulate infection using immutable 16 by 16 bitboard tiles, retaining the active tile between boundary crossings
// cost: O(n + b + c log t) time, O(n + t) live memory for b bursts, c tile crossings and t tiles; two text passes and one parsed-cell traversal
// trade: spatial locality amortizes persistent-map updates across bursts within the active tile
module Aoc.Y2017.Day22.Primary

open Aoc.Solution
open Aoc.Y2017.Day22.Input

type Node =
    | Clean
    | Weakened
    | Infected
    | Flagged

module Tile =
    [<Struct>]
    type Tile =
        {
            W0: uint64
            W1: uint64
            W2: uint64
            W3: uint64
            W4: uint64
            W5: uint64
            W6: uint64
            W7: uint64
        }

    let empty =
        {
            W0 = 0UL
            W1 = 0UL
            W2 = 0UL
            W3 = 0UL
            W4 = 0UL
            W5 = 0UL
            W6 = 0UL
            W7 = 0UL
        }

    let inline private word index tile =
        match index with
        | 0 -> tile.W0
        | 1 -> tile.W1
        | 2 -> tile.W2
        | 3 -> tile.W3
        | 4 -> tile.W4
        | 5 -> tile.W5
        | 6 -> tile.W6
        | _ -> tile.W7

    let inline get x y tile =
        let cell = ((y &&& 15) <<< 4) ||| (x &&& 15)

        match (word (cell >>> 5) tile >>> ((cell &&& 31) <<< 1)) &&& 3UL with
        | 0UL -> Clean
        | 1UL -> Weakened
        | 2UL -> Infected
        | _ -> Flagged

    let inline set x y state tile =
        let cell = ((y &&& 15) <<< 4) ||| (x &&& 15)
        let index = cell >>> 5
        let shift = (cell &&& 31) <<< 1

        let encoded =
            match state with
            | Clean -> 0UL
            | Weakened -> 1UL
            | Infected -> 2UL
            | Flagged -> 3UL

        let value = (word index tile &&& ~~~(3UL <<< shift)) ||| (encoded <<< shift)

        match index with
        | 0 -> { tile with W0 = value }
        | 1 -> { tile with W1 = value }
        | 2 -> { tile with W2 = value }
        | 3 -> { tile with W3 = value }
        | 4 -> { tile with W4 = value }
        | 5 -> { tile with W5 = value }
        | 6 -> { tile with W6 = value }
        | _ -> { tile with W7 = value }

    let key x y =
        // Mask after widening: uint32 conversion of negative coordinates throws in checked builds.
        (int64 (x >>> 4) <<< 32) ||| (int64 (y >>> 4) &&& 0xffffffffL)

type private Mode =
    | Basic
    | Evolved

let rec private run mode remaining x y dx dy count key tile tiles =
    if remaining <= 0 then
        count
    else
        let nextKey = Tile.key x y

        if nextKey <> key then
            let saved = Map.add key tile tiles
            let active = Map.tryFind nextKey saved |> Option.defaultValue Tile.empty
            run mode remaining x y dx dy count nextKey active saved
        else
            let state = Tile.get x y tile

            let nextDx, nextDy =
                match state with
                | Clean -> dy, -dx
                | Weakened -> dx, dy
                | Infected -> -dy, dx
                | Flagged -> -dx, -dy

            let next =
                match mode, state with
                | Basic, Clean -> Infected
                | Basic, Infected -> Clean
                | Basic, Weakened
                | Evolved, Weakened -> Infected
                | Basic, Flagged
                | Evolved, Flagged -> Clean
                | Evolved, Clean -> Weakened
                | Evolved, Infected -> Flagged

            run
                mode
                (remaining - 1)
                (x + nextDx)
                (y + nextDy)
                nextDx
                nextDy
                (count + (if next = Infected then 1 else 0))
                key
                (Tile.set x y next tile)
                tiles

let internal solveWith basic evolved : Solver =
    fun input ->
        Input.parse input
        |> Result.map (fun (InfectionMap rows) ->
            let centreY = rows.Length / 2

            let tiles =
                [
                    for y, row in List.indexed rows do
                        for x, node in List.indexed row do
                            if node = Input.Infected then
                                yield x - row.Length / 2, y - centreY
                ]
                |> List.fold
                    (fun tiles (x, y) ->
                        let key = Tile.key x y
                        let tile = Map.tryFind key tiles |> Option.defaultValue Tile.empty
                        Map.add key (Tile.set x y Infected tile) tiles)
                    Map.empty

            let active = Map.tryFind 0L tiles |> Option.defaultValue Tile.empty

            Answers.both
                (run Basic basic 0 0 0 -1 0 0L active tiles |> string)
                (run Evolved evolved 0 0 0 -1 0 0L active tiles |> string))

let solve: Solver = solveWith 10000 10000000
