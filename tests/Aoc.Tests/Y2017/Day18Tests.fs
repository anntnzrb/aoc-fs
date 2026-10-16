module Aoc.Tests.Y2017.Day18Tests

open Expecto
open FsCheck.FSharp
open Aoc.Solution
open Aoc.Y2017.Day18

let private programs =
    let register = Gen.elements [ 'a' .. 'd' ] |> Gen.map Input.Register

    let operand =
        Gen.oneof
            [
                Gen.choose (-8, 8) |> Gen.map (int64 >> Input.Literal)
                register |> Gen.map Input.Read
            ]

    let renderOperand =
        function
        | Input.Literal number -> string number
        | Input.Read(Input.Register name) -> string name

    let instruction =
        Gen.oneof
            [
                operand
                |> Gen.map (fun value -> $"snd {renderOperand value}", Input.Sound value)
                register
                |> Gen.map (fun (Input.Register name as target) -> $"rcv {name}", Input.Receive target)
                gen {
                    let! (Input.Register name as target) = register

                    let! operation, keyword =
                        Gen.elements
                            [
                                Input.Assign, "set"
                                Input.Increase, "add"
                                Input.Product, "mul"
                                Input.Remainder, "mod"
                            ]

                    let! value = operand
                    return $"{keyword} {name} {renderOperand value}", Input.Update(target, operation, value)
                }
                gen {
                    let! condition = operand
                    // Forward jumps keep both interpreters finite for totality checks.
                    let! offset = Gen.choose (1, 4)
                    return $"jgz {renderOperand condition} {offset}", Input.Jump(condition, Input.Literal(int64 offset))
                }
            ]

    Gen.nonEmptyListOf instruction
    |> Gen.map (fun instructions -> instructions |> List.map fst |> String.concat "\n", List.map snd instructions)

[<Tests>]
let tests =
    testList
        "Y2017.Day18"
        [
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 40
                    endSize = 12
                }
                "parser preserves generated instructions"
                (Prop.forAll (Arb.fromGen programs) (fun (text, expected) -> Input.parse text = Ok expected))
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 40
                    endSize = 12
                }
                "solver is total on terminating generated programs"
                (Prop.forAll (Arb.fromGen programs) (fun (text, _) ->
                    Primary.solve text |> ignore
                    true))
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
