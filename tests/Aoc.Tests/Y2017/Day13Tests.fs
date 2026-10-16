module Aoc.Tests.Y2017.Day13Tests

open Expecto
open Aoc.Y2017.Day13

[<Tests>]
let tests =
    testList
        "Y2017.Day13"
        [
            testCase "stationary scanner has no safe delay" (fun () ->
                Expect.equal (Primary.solve "3: 1\n") (Error "no delay passes the firewall") "stationary scanner")
            testCase "exhausted joint cycle has no safe delay" (fun () ->
                Expect.equal
                    (Primary.solve "0: 2\n1: 2\n")
                    (Error "no delay passes the firewall")
                    "both delay parities caught")
        ]
