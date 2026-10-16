// approach: index every rule's rotations and reflections, then replace square blocks and reassemble the image
// cost: O(r log r + a log r) time, O(r + a) memory for r rules and a final pixels; one input pass and one image traversal per iteration
// trade: a dense grid follows the geometric rules directly; a symmetry index avoids searching rules for every block
module Aoc.Y2017.Day21.Primary

open Aoc.Solution
open Aoc.Y2017.Day21.Input

let private grid (rows: string list) =
    let rows = rows |> List.toArray
    Array2D.init rows.Length rows.Length (fun y x -> rows[y][x] = '#')

let private key size top left (image: bool[,]) =
    let mutable bits = 0

    for y in 0 .. size - 1 do
        for x in 0 .. size - 1 do
            bits <- (bits <<< 1) ||| (if image[top + y, left + x] then 1 else 0)

    size, bits

let private index rules =
    rules
    |> List.fold
        (fun lookup rule ->
            let source = grid rule.Source
            let target = grid rule.Target
            let size = Array2D.length1 source

            let rotate (image: bool[,]) =
                Array2D.init size size (fun y x -> image[size - 1 - x, y])

            let flip (image: bool[,]) =
                Array2D.init size size (fun y x -> image[y, size - 1 - x])

            [ 0..3 ]
            |> List.fold
                (fun (lookup, image) _ ->
                    let lookup =
                        lookup
                        |> Map.add (key size 0 0 image) target
                        |> Map.add (key size 0 0 (flip image)) target

                    lookup, rotate image)
                (lookup, source)
            |> fst)
        Map.empty

let private enhance (lookup: Map<int * int, bool[,]>) (image: bool[,]) =
    let size = Array2D.length1 image
    let block = if size % 2 = 0 then 2 else 3
    let blocks = size / block
    let output = Array2D.zeroCreate<bool> (blocks * (block + 1)) (blocks * (block + 1))

    [
        for y in 0 .. blocks - 1 do
            for x in 0 .. blocks - 1 -> y, x
    ]
    |> List.fold
        (fun result (y, x) ->
            result
            |> Result.bind (fun () ->
                match lookup |> Map.tryFind (key block (y * block) (x * block) image) with
                | None -> Error "no enhancement rule matches a block"
                | Some target ->
                    for dy in 0..block do
                        for dx in 0..block do
                            output[y * (block + 1) + dy, x * (block + 1) + dx] <- target[dy, dx]

                    Ok()))
        (Ok())
    |> Result.map (fun () -> output)

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.bind (fun rules ->
            let lookup = index rules
            let start = grid [ ".#."; "..#"; "###" ]

            [ 1..5 ]
            |> List.fold (fun image _ -> image |> Result.bind (enhance lookup)) (Ok start)
            |> Result.map (fun image ->
                let mutable count = 0

                image
                |> Array2D.iter (fun on ->
                    if on then
                        count <- count + 1)

                Answers.both (string count) "0"))
