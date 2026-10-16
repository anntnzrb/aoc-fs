// approach: test arrival times against scanner periods, then search increasing delays within their joint repeating cycle
// cost: O(n * min(d+1, L)) arithmetic operations, O(n) bigint values; parsing, timing, severity, period checks and LCM passes, then short-circuit scans per delay; d is the first safe delay (L if none), L the periods' LCM
// trade: modular timing avoids simulation; bigint keeps the finite LCM bound exact even when periods multiply beyond machine integers
module Aoc.Y2017.Day13.Primary

open Aoc.Solution
open Aoc.Y2017.Day13.Input

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.bind (fun layers ->
            let timings =
                layers
                |> List.map
                    (fun
                        {
                            Depth = Depth depth
                            Range = Range range
                        } ->
                        let depth, range = bigint depth, bigint range
                        depth, range, 2I * (range - 1I))

            let severity =
                timings
                |> List.sumBy (fun (depth, range, period) ->
                    if period = 0I || depth % period = 0I then
                        depth * range
                    else
                        0I)

            if timings |> List.exists (fun (_, _, period) -> period = 0I) then
                Error "no delay passes the firewall"
            else
                let cycle =
                    timings
                    |> List.fold
                        (fun cycle (_, _, period) ->
                            cycle / System.Numerics.BigInteger.GreatestCommonDivisor(cycle, period) * period)
                        1I

                Seq.unfold (fun delay -> if delay < cycle then Some(delay, delay + 1I) else None) 0I
                |> Seq.tryFind (fun delay ->
                    timings
                    |> List.forall (fun (depth, _, period) -> (delay + depth) % period <> 0I))
                |> function
                    | Some delay -> Ok(Answers.both (string severity) (string delay))
                    | None -> Error "no delay passes the firewall")
