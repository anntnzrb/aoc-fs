module Aoc.Tests.Y2017.Day23Tests

open Expecto
open Aoc.Y2017.Day23

[<Tests>]
let tests =
    testList
        "Y2017.Day23"
        [
            testCase "rejects unsupported factor-loop structure" (fun () ->
                Expect.equal
                    (Primary.solve "set d 2\nset e 2\n")
                    (Error "unsupported nested factor-loop structure")
                    "A candidate loop must satisfy the complete structural proof")
            testCase "rejects registers outside the machine" (fun () ->
                Expect.isError (Input.parse "set z 1\n") "Only registers a through h exist")
        ]
