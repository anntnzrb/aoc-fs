/// Pure solver contracts and answer rendering, independent of IO.
module Aoc.Solution

/// Answers in part order; absent part 2 means there is no computational second part.
type Answers =
    private
        {
            /// The first answer, rendered before part 2.
            Part1: string
            /// The optional second answer.
            Part2: string option
        }

/// Constructs answers and renders them in part order.
module Answers =
    /// Constructs a single-part result.
    let part1 (answer: string) : Answers = { Part1 = answer; Part2 = None }

    /// Constructs a two-part result, including any unfinished-part placeholder.
    let both (part1: string) (part2: string) : Answers = { Part1 = part1; Part2 = Some part2 }

    /// Emits one answer per line, each terminated by LF, with part 1 first.
    let render (answers: Answers) : string =
        match answers.Part2 with
        | None -> answers.Part1 + "\n"
        | Some part2 -> answers.Part1 + "\n" + part2 + "\n"

/// Transforms input text into answers or a parse/domain error message.
type Solver = string -> Result<Answers, string>

/// Selects the primary implementation or an exploration by its lowercase CLI name.
type Variant =
    /// The day's first, idiomatic implementation.
    | Primary
    /// An alternative implementation identified by its lowercase CLI name.
    | Exploration of string

/// Identifies one implementation of a puzzle by year, day and variant.
type Key =
    {
        /// The Advent of Code year.
        Year: int
        /// The one-based puzzle day within the year's calendar.
        Day: int
        /// The selected implementation.
        Variant: Variant
    }
