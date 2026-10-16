module Aoc.Tests.Y2017.Day12Tests

open Expecto
open Aoc.Y2017.Day12

[<Tests>]
let tests =
    testList
        "Y2017.Day12"
        [
            testCase "missing program zero has no group to count" (fun () ->
                // Only program 1 exists; seeding a traversal with 0 would invent a program.
                Expect.equal (Primary.solve "1 <-> 1\n") (Error "program 0 is not declared") "missing program zero")
            testCase "mentioning program zero does not declare it" (fun () ->
                // Program 0 appears only as a connection, not as a declared program.
                Expect.equal (Primary.solve "1 <-> 0\n") (Error "program 0 is not declared") "undeclared connection")
        ]
