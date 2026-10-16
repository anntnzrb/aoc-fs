module Aoc.Tests.Y2017.Day22Tests

open Expecto
open Aoc.Y2017.Day22

[<Tests>]
let tests =
    testList
        "Y2017.Day22"
        [
            testCase "parser preserves rectangular row and cell order" (fun () ->
                // A 5-column, 3-row map has a unique centre; CRLF is only a line separator.
                let actual =
                    Input.parse "#....\r\n..#..\r\n....#\r\n"
                    |> Result.map (fun (Input.InfectionMap rows) ->
                        [
                            for y, row in List.indexed rows do
                                for x, node in List.indexed row do
                                    if node = Input.Infected then
                                        yield x, y
                        ])

                Expect.equal actual (Ok [ 0, 0; 2, 1; 4, 2 ]) "coordinates retain source order")
            testCase "parser rejects maps without a unique rectangular centre" (fun () ->
                // Empty, even-width, even-height and ragged maps violate the domain shape.
                for input in [ ""; "..\n"; ".\n.\n"; "...\n.\n...\n" ] do
                    Expect.equal (Input.parse input) (Error "expected an odd-sized rectangular map") "invalid shape")
            testCase "parser rejects unknown cells" (fun () ->
                Expect.equal (Input.parse "x\n") (Error "expected a clean or infected node") "invalid cell")
        ]
