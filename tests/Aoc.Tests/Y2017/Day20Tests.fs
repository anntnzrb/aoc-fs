module Aoc.Tests.Y2017.Day20Tests

open Expecto
open Aoc.Solution
open Aoc.Y2017.Day20

[<Tests>]
let tests =
    testList
        "Y2017.Day20"
        [
            testCase "eventual sign follows the first nonzero coefficient" (fun () ->
                // Stationary distances are 5 and 10. Moving distances eventually grow
                // at rates 1 and 2; 5-t = 2t has no integer meeting time.
                for input in
                    [
                        "p=<-5,0,0>, v=<0,0,0>, a=<0,0,0>\np=<10,0,0>, v=<0,0,0>, a=<0,0,0>\n"
                        "p=<5,0,0>, v=<-1,0,0>, a=<0,0,0>\np=<0,0,0>, v=<2,0,0>, a=<0,0,0>\n"
                    ] do
                    Expect.equal (Primary.solve input |> Result.map Answers.render) (Ok "0\n2\n") "Eventual ranking"

                // Accelerations -1 and -2 rank particle 0 closest eventually;
                // -t(t+1)/2 = 10-t(t+1) at t=4 removes both particles.
                let accelerated =
                    "p=<0,0,0>, v=<0,0,0>, a=<-1,0,0>\np=<10,0,0>, v=<0,0,0>, a=<-2,0,0>\n"

                Expect.equal
                    (Primary.solve accelerated |> Result.map Answers.render)
                    (Ok "0\n0\n")
                    "Negative acceleration fixes eventual sign")
            testCase "collisions require agreement on all spatial axes" (fun () ->
                // The x coordinates agree at t=1, but the fixed y coordinates differ.
                // Both distances grow at rate 1; particle 1's eventual constant is -1.
                let input = "p=<0,0,0>, v=<1,0,0>, a=<0,0,0>\np=<2,1,0>, v=<-1,0,0>, a=<0,0,0>\n"

                Expect.equal
                    (Primary.solve input |> Result.map Answers.render)
                    (Ok "1\n2\n")
                    "One agreeing axis does not imply collision")
        ]
