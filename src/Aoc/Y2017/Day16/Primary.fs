// approach: fold the recorded rotations, position swaps and name swaps over the initial lineup
// cost: O(m*n) time, O(m+n) memory for m moves and n=16 programs; one input pass
// trade: fresh arrays keep each move explicit and the dance a pure transformation
module Aoc.Y2017.Day16.Primary

open Aoc.Solution
open Aoc.Y2017.Day16.Input

let private dance moves lineup =
    moves
    |> List.fold
        (fun (programs: char array) move ->
            match move with
            | Spin count ->
                Array.init programs.Length (fun i ->
                    programs[(i + programs.Length - count % programs.Length) % programs.Length])
            | Exchange(a, b) ->
                programs
                |> Array.mapi (fun i name ->
                    if i = a then programs[b]
                    elif i = b then programs[a]
                    else name)
            | Partner(a, b) ->
                programs
                |> Array.map (fun name ->
                    if name = a then b
                    elif name = b then a
                    else name))
        lineup

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun moves ->
            let programs = [| 'a' .. 'p' |] |> dance moves |> System.String
            Answers.both programs "0")
