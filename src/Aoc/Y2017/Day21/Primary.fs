// approach: index every rule's rotations and reflections, then replace square blocks and reassemble the image
// cost: O(r log r + a log r) time, O(r + a) memory for r rules and a final pixels; one input pass, block reads and output writes per iteration, two counting passes
// trade: a dense grid follows the geometric rules directly; a symmetry index avoids searching rules for every block
module Aoc.Y2017.Day21.Primary

open Aoc.Solution
open Aoc.Y2017.Day21.Input
open FsToolkit.ErrorHandling

let private grid (rows: string list) =
    let rows = rows |> List.toArray
    Array2D.init rows.Length rows.Length (fun y x -> rows[y][x] = '#')

let private key size top left (image: bool[,]) =
    let bits =
        [
            for y in 0 .. size - 1 do
                for x in 0 .. size - 1 -> y, x
        ]
        |> List.fold (fun bits (y, x) -> (bits <<< 1) ||| (if image[top + y, left + x] then 1 else 0)) 0

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

            source
            |> Seq.unfold (fun image -> Some(image, rotate image))
            |> Seq.take 4
            |> Seq.fold
                (fun lookup image ->
                    lookup
                    |> Map.add (key size 0 0 image) target
                    |> Map.add (key size 0 0 (flip image)) target)
                lookup)
        Map.empty

let private enhance (lookup: Map<int * int, bool[,]>) (image: bool[,]) =
    let size = Array2D.length1 image
    let block = if size % 2 = 0 then 2 else 3
    let blocks = size / block

    [
        for y in 0 .. blocks - 1 do
            for x in 0 .. blocks - 1 -> y, x
    ]
    |> Aoc.Parse.traverse (fun (y, x) ->
        lookup
        |> Map.tryFind (key block (y * block) (x * block) image)
        |> Result.requireSome "no enhancement rule matches a block")
    |> Result.map (fun targets ->
        let targets = targets |> List.toArray
        let expanded = block + 1
        let outputSize = blocks * expanded

        Array2D.init outputSize outputSize (fun y x ->
            let target = targets[(y / expanded) * blocks + x / expanded]
            target[y % expanded, x % expanded]))

let private count image =
    image |> Seq.cast<bool> |> Seq.sumBy (fun on -> if on then 1 else 0)

let solve: Solver =
    fun input ->
        result {
            let! rules = Input.parse input
            let lookup = index rules
            let start = grid [ ".#."; "..#"; "###" ]
            let part1Iterations = 5
            let part2Iterations = 18

            let images =
                [ 1..part2Iterations ]
                |> Seq.scan (fun image _ -> image |> Result.bind (enhance lookup)) (Ok start)
                |> Seq.toArray

            let! part1 = images[part1Iterations]
            let! part2 = images[part2Iterations]
            return Answers.both (count part1 |> string) (count part2 |> string)
        }
