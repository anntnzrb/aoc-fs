module Aoc.Tests.Y2017.Day16Tests

open Expecto
open Aoc.Solution
open Aoc.Y2017.Day16

[<Tests>]
let tests =
    testList
        "Y2017.Day16"
        [
            testCase "parses all move forms in order with outer whitespace" (fun () ->
                match Input.parse " \ns0,x0/15,pa/p\r\n " with
                | Ok [ Input.Spin 0; Input.Exchange(0, 15); Input.Partner('a', 'p') ] -> ()
                | result -> failtestf "unexpected parse: %A" result)
            testCase "rejects empty dances and incomplete comma-separated moves" (fun () ->
                for input in [ ""; " \n"; ",s1"; "s1,"; "s1,,s2"; "s1junk"; "s1x0/1"; "s1, s2" ] do
                    Expect.isError (Input.parse input) $"reject {input}")
            testCase "rejects invalid move operands" (fun () ->
                for input in
                    [
                        "s-1"
                        "s2147483648"
                        "s"
                        "x-1/0"
                        "x0/16"
                        "x16/0"
                        "x0/"
                        "x0/1/2"
                        "pA/a"
                        "pa/q"
                        "pq/a"
                        "pa/"
                        "q0"
                    ] do
                    Expect.isError (Input.parse input) $"reject {input}")
            testCase "equal swap operands leave both answers unchanged" (fun () ->
                // Swapping any position or name with itself is identity, including after a billion dances.
                let expected = Ok(Answers.both "abcdefghijklmnop" "abcdefghijklmnop")

                for input in [ "x0/0"; "x15/15"; "pa/a"; "pp/p"; "s16,x0/0,pp/p" ] do
                    Expect.equal (Primary.solve input) expected input)
            testCase "boundary swaps exchange both ends and square to identity" (fun () ->
                // Both swaps exchange a and p; an even billion repetitions restore their original order.
                let expected = Ok(Answers.both "pbcdefghijklmnoa" "abcdefghijklmnop")

                for input in [ "x0/15"; "x15/0"; "pa/p"; "pp/a" ] do
                    Expect.equal (Primary.solve input) expected input)
        ]
