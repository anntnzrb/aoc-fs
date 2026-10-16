module Aoc.Tests.Y2017.Day18Tests

open Expecto
open Aoc.Solution
open Aoc.Y2017.Day18

[<Tests>]
let tests =
    testList
        "Y2017.Day18"
        [
            testCase "minimum integer modulo minus one is zero" (fun () ->
                // Every integer is divisible by -1: recover 0; each duet copy sends once.
                let program = "set a -9223372036854775808\nmod a -1\nsnd a\nset b 1\nrcv b\n"
                Expect.equal (Primary.solve program) (Ok(Answers.both "0" "1")) "exact remainder")
            testCase "register overflow returns an error" (fun () ->
                // Each operation's mathematical result lies beyond one signed 64-bit boundary.
                for program in
                    [
                        "set a 9223372036854775807\nadd a 1\n"
                        "set a -9223372036854775808\nadd a -1\n"
                        "set a 9223372036854775807\nmul a 2\n"
                        "set a -9223372036854775808\nmul a -1\n"
                        // Recovery stops before the update; duet's p=1 copy overflows.
                        "snd 1\nset b 1\nrcv b\nset a 9223372036854775807\nadd a p\n"
                    ] do
                    Expect.equal (Primary.solve program) (Error "register overflow") "out-of-range register")
        ]
