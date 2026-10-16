/// Advent of Code puzzle dates, with years before 2015 excluded.
module Aoc.Calendar

/// Returns 25 days for 2015 through 2024, 12 thereafter, and zero before 2015.
let days (year: int) : int =
    match year with
    | year when year < 2015 -> 0
    | year when year <= 2024 -> 25
    | _ -> 12

/// Returns whether day is in the inclusive puzzle-day range for year.
let isPuzzleDay (year: int) (day: int) : bool = day >= 1 && day <= days year
