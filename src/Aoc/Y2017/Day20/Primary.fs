// approach: rank the eventual Manhattan-distance polynomials by their quadratic, linear and constant coefficients
// cost: O(n) time and memory, one input parse and one particle traversal
// trade: exact asymptotic ordering avoids choosing an arbitrary simulation duration
module Aoc.Y2017.Day20.Primary

open Aoc.Solution
open Aoc.Y2017.Day20.Input

let private distancePolynomial particle =
    let (Vector(px, py, pz)) = particle.Position
    let (Vector(vx, vy, vz)) = particle.Velocity
    let (Vector(ax, ay, az)) = particle.Acceleration

    // Twice the position is a*t² + (2*v+a)*t + 2*p; its leading nonzero term fixes its eventual sign.
    [ px, vx, ax; py, vy, ay; pz, vz, az ]
    |> List.fold
        (fun (quadratic, linear, constant) (p, v, a) ->
            let direction =
                sign (
                    if a <> 0L then a
                    elif v <> 0L then v
                    else p
                )
                |> int64

            quadratic + direction * a, linear + direction * (2L * v + a), constant + direction * 2L * p)
        (0L, 0L, 0L)

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.bind (fun particles ->
            particles
            |> List.indexed
            |> List.fold
                (fun closest (index, particle) ->
                    let candidate = distancePolynomial particle, index

                    match closest with
                    | Some best -> Some(min best candidate)
                    | None -> Some candidate)
                None
            |> function
                | Some(_, index) -> Ok(Answers.both (string index) "0")
                | None -> Error "no particles")
