module Aoc.Tests.Y2017.Day07Tests

open Expecto
open Aoc.Solution
open Aoc.Y2017.Day07

[<Tests>]
let tests =
    testList
        "Y2017.Day07"
        [
            testCase "preserves program and child order with trimmed CRLF input" (fun () ->
                let parsed =
                    Input.parse " \t\r\nroot (0) -> z, a\r\nz (01)\r\na (2147483647)\r\n\r\n\t "
                    |> Result.map (List.map (fun program -> program.Name, program.Weight, program.Children))

                Expect.equal
                    parsed
                    (Ok [ ("root", 0, [ "z"; "a" ]); ("z", 1, []); ("a", 2147483647, []) ])
                    "outer whitespace is trimmed; ordering and nonnegative Int32 weights are retained")
            testCase "rejects invalid names weights and separators" (fun () ->
                for text in
                    [
                        "Root (1)"
                        "root (1) -> Child"
                        "root (-1)"
                        "root (+1)"
                        "root ()"
                        "root (١)"
                        "root (2147483648)"
                        "root  (1)"
                        "root\t(1)"
                        "root (1) -> a,b"
                        "root (1) -> "
                        "root (1)\n\na (1)"
                        "root (1)\ra (1)"
                        "root (1)\na (1) trailing"
                    ] do
                    Expect.isError (Input.parse text) $"must reject {text}")
            testCase "rejects empty towers" (fun () ->
                Expect.equal (Input.parse " \r\n\t ") (Error "empty tower") "no program descriptions")
            testCase "rejects duplicate program names" (fun () ->
                Expect.equal
                    (Input.parse "root (1)\nroot (2)\n")
                    (Error "duplicate program name")
                    "names identify unique programs")
            testCase "corrects either child using the inherited tower total" (fun () ->
                // a and b total 10, so c must total 10; with c's own weight 0, both children must weigh 5.
                for children in [ "d, e"; "e, d" ] do
                    let input = $"r (0) -> a, b, c\na (10)\nb (10)\nc (0) -> {children}\nd (5)\ne (7)\n"

                    Expect.equal
                        (Primary.solve input)
                        (Ok(Answers.both "r" "5"))
                        "child order cannot change correction")
            testCase "rejects ambiguous sibling weights" (fun () ->
                // Without a parent target, either leaf could be changed to match the other.
                Expect.equal
                    (Primary.solve "r (0) -> a, b\na (5)\nb (7)\n")
                    (Error "ambiguous weight correction")
                    "two unequal children alone do not identify the wrong weight")
        ]
