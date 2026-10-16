// approach: separate commuting position and name permutations, then square each to repeat the dance
// cost: O(m*n+n*log k) time, O(m+n) memory; one input pass and three move-list passes
// trade: permutation powers avoid replaying k dances or storing a history; n=16 and k=one billion
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

let private power count (permutation: char array) =
    let rec loop remaining result (squared: char array) =
        if remaining <= 0 then
            result
        else
            let nextResult =
                if remaining % 2 = 1 then
                    result |> Array.map (fun name -> squared[int name - int 'a'])
                else
                    result

            let nextSquared = squared |> Array.map (fun name -> squared[int name - int 'a'])
            loop (remaining / 2) nextResult nextSquared

    loop count [| 'a' .. 'p' |] permutation

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun moves ->
            let initial = [| 'a' .. 'p' |]
            let first = initial |> dance moves |> System.String

            // Renaming commutes with moving positions, so the two permutations can be powered separately.
            let positions, names =
                moves
                |> List.partition (function
                    | Partner _ -> false
                    | Spin _
                    | Exchange _ -> true)

            let positionPower = initial |> dance positions |> power 1000000000
            let namePower = initial |> dance names |> power 1000000000

            let billionth =
                positionPower
                |> Array.map (fun name -> namePower[int name - int 'a'])
                |> System.String

            Answers.both first billionth)
