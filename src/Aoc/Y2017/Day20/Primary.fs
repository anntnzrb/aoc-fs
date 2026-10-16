// approach: rank eventual distance polynomials; solve pairwise collision times and remove simultaneous live participants
// cost: O(n² log C + e log e + e log n) time, O(n + e) memory for e collision events and coefficient magnitude C; one input parse
// trade: exact polynomial arithmetic avoids simulation cutoffs; bigint protects discriminants and event times from overflow
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

let private collisionTimes first second =
    let roots (p: int64) (v: int64) (a: int64) =
        let quadratic, b, c = bigint a, 2I * bigint v + bigint a, 2I * bigint p

        let integralRoots denominator numerators =
            numerators
            |> List.choose (fun numerator ->
                if numerator % denominator = 0I && numerator / denominator >= 0I then
                    Some(numerator / denominator)
                else
                    None)
            |> Set.ofList
            |> Some

        match a, v, p with
        | 0L, 0L, 0L -> None // This axis agrees at every time.
        | 0L, 0L, _ -> Some Set.empty
        | 0L, _, _ -> integralRoots b [ -c ]
        | _ ->
            let discriminant = b * b - 4I * quadratic * c

            if discriminant < 0I then
                Some Set.empty
            else
                let mutable root = discriminant
                let mutable next = (root + 1I) / 2I

                while next < root do
                    root <- next
                    next <- (root + discriminant / root) / 2I

                if root * root = discriminant then
                    integralRoots (2I * quadratic) [ -b - root; -b + root ]
                else
                    Some Set.empty

    let (Vector(px, py, pz)), (Vector(qx, qy, qz)) = first.Position, second.Position
    let (Vector(vx, vy, vz)), (Vector(wx, wy, wz)) = first.Velocity, second.Velocity

    let (Vector(ax, ay, az)), (Vector(bx, by, bz)) =
        first.Acceleration, second.Acceleration

    [
        px - qx, vx - wx, ax - bx
        py - qy, vy - wy, ay - by
        pz - qz, vz - wz, az - bz
    ]
    |> List.fold
        (fun times (p, v, a) ->
            match times with
            | Some ts when Set.isEmpty ts -> times
            | _ ->
                match times, roots p v a with
                | None, other
                | other, None -> other
                | Some left, Some right -> Some(Set.intersect left right))
        None
    |> Option.defaultValue (Set.singleton 0I)

let private survivors particles =
    let _, events =
        particles
        |> List.indexed
        |> List.fold
            (fun (seen, events) (index, particle) ->
                let meetings =
                    [
                        for otherIndex, other in seen do
                            for time in collisionTimes particle other do
                                yield time, (index, otherIndex)
                    ]

                (index, particle) :: seen, meetings @ events)
            ([], [])

    events
    |> List.groupBy fst
    |> List.sortBy fst
    |> List.fold
        (fun alive (_, meetings) ->
            // Test every pair against the same live set before removing the whole tick's collisions.
            let destroyed =
                [
                    for _, (left, right) in meetings do
                        if Set.contains left alive && Set.contains right alive then
                            yield left
                            yield right
                ]
                |> Set.ofList

            Set.difference alive destroyed)
        (Set.ofList [ 0 .. List.length particles - 1 ])
    |> Set.count

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
                | Some(_, index) -> Ok(Answers.both (string index) (survivors particles |> string))
                | None -> Error "no particles")
