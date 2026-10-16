// approach: advance both generators and count pairs whose low 16 bits agree
// cost: O(p) time for p pairs, O(1) memory, one input pass
// trade: local mutable state keeps the recurrence direct without storing generated values
module Aoc.Y2017.Day15.Primary

open Aoc.Solution
open Aoc.Y2017.Day15.Input

let private matches pairs (Seed startA) (Seed startB) =
    let mutable a = startA
    let mutable b = startB
    let mutable count = 0

    for _ in 1..pairs do
        a <- a * 16807L % 2147483647L
        b <- b * 48271L % 2147483647L

        if (a &&& 65535L) = (b &&& 65535L) then
            count <- count + 1

    count

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun generators -> Answers.both (matches 40000000 generators.A generators.B |> string) "0")
