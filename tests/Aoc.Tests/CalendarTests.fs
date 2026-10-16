module Aoc.Tests.CalendarTests

open Expecto
open Aoc

[<Tests>]
let tests =
    testList
        "Calendar"
        [
            for year, expected in [ 2014, 0; 2015, 25; 2024, 25; 2025, 12 ] do
                testCase (sprintf "%d day count" year) (fun () ->
                    Expect.equal (Calendar.days year) expected "Calendar range")
            for year, day, expected in
                [
                    2017, 0, false
                    2017, 1, true
                    2017, 25, true
                    2017, 26, false
                    2025, 12, true
                    2025, 13, false
                ] do
                testCase (sprintf "%d day %d validity" year day) (fun () ->
                    Expect.equal (Calendar.isPuzzleDay year day) expected "Puzzle day boundary")
        ]
