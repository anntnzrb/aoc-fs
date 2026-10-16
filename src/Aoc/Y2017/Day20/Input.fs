/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day20.Input

open FParsec

/// Integer coordinates along the three spatial axes.
type Vector = Vector of x: int64 * y: int64 * z: int64

/// A particle's initial position, velocity and constant acceleration.
type Particle =
    {
        Position: Vector
        Velocity: Vector
        Acceleration: Vector
    }

/// Parses a non-empty list of particles in their identifying input order.
let parse (input: string) : Result<Particle list, string> =
    let coordinate = spaces >>. pint32 .>> spaces |>> int64

    let vector =
        between (pchar '<') (pchar '>') (tuple3 (coordinate .>> pchar ',') (coordinate .>> pchar ',') coordinate)
        |>> Vector

    let particle =
        tuple3
            (pstring "p=" >>. vector .>> pstring ", ")
            (pstring "v=" >>. vector .>> pstring ", ")
            (pstring "a=" >>. vector)
        |>> fun (position, velocity, acceleration) ->
            {
                Position = position
                Velocity = velocity
                Acceleration = acceleration
            }

    Aoc.Parse.run (sepEndBy1 particle newline .>> eof) input
