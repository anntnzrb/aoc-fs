module Aoc.Tests.SolutionTests

open Expecto
open System.Text.RegularExpressions
open Aoc.Solution

[<Tests>]
let tests =
    testList
        "Solution"
        [
            testCase "one answer renders with trailing LF" (fun () ->
                Expect.equal (Answers.part1 "42" |> Answers.render) "42\n" "Part 1 output")
            testCase "both answers render in order" (fun () ->
                Expect.equal (Answers.both "1" "2" |> Answers.render) "1\n2\n" "Part order")
            testCase "registered keys have valid dates and variant names" (fun () ->
                for KeyValue(key, _) in Aoc.Registry.solvers do
                    Expect.isTrue (Aoc.Calendar.isPuzzleDay key.Year key.Day) "Valid puzzle date"

                    match key.Variant with
                    | Primary -> ()
                    | Exploration name ->
                        Expect.isTrue (Regex.IsMatch(name, @"\A[a-z][a-z0-9]*\z")) "Lowercase CLI variant")
        ]
