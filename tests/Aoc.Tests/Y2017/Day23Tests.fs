module Aoc.Tests.Y2017.Day23Tests

open Expecto
open Aoc.Solution
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
            // The subtractions cross either int64 boundary; both products are outside that range.
            for name, program in
                [
                    "subtraction above maximum", "set h 9223372036854775807\nsub h -1\n"
                    "subtraction below minimum", "set h -9223372036854775808\nsub h 1\n"
                    "multiplication above maximum", "set h -9223372036854775808\nmul h -1\n"
                    "multiplication below minimum", "set h 9223372036854775807\nmul h -2\n"
                ] do
                testCase name (fun () ->
                    Expect.equal (Primary.solve program) (Error "register overflow") "Overflow returns an error")
            testCase "rejects overflow when only part two executes it" (fun () ->
                // a=0 skips the arithmetic; a=1 reaches MaxValue + 1.
                Expect.equal
                    (Primary.solve "jnz a 2\njnz 1 3\nset h 9223372036854775807\nsub h -1\n")
                    (Error "register overflow")
                    "Both interpreter runs use the same overflow policy")
            testCase "accepts arithmetic at the integer boundaries" (fun () ->
                // MinValue - (-1) - (-MaxValue) = 0; MaxValue * 1 stays representable.
                let program =
                    "set h -9223372036854775808\nsub h -1\nsub h -9223372036854775807\nset h 9223372036854775807\nmul h 1\n"

                Expect.equal
                    (Primary.solve program |> Result.map Answers.render)
                    (Ok "1\n9223372036854775807\n")
                    "Representable boundary arithmetic is preserved")
        ]
