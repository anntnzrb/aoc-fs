/// Circular reversal hashing shared by puzzle solvers; callers validate inputs before hashing.
module Aoc.KnotHash

/// Returns a fresh 256-mark permutation after the given nonnegative number of rounds.
/// Lengths must be in 0..256; position and skip carry across rounds, with circular indexing.
/// Zero rounds or no lengths leave marks in ascending order; invalid arguments are outside the contract.
let sparse rounds lengths =
    let step (marks: int array, position, skip) length =
        let next =
            Array.init 256 (fun index ->
                let offset = (index - position + 256) % 256

                if offset < length then
                    marks[(position + length - 1 - offset) % 256]
                else
                    marks[index])

        // Only the skip's residue affects circular positions, including across rounds.
        next, (position + length + skip) % 256, (skip + 1) % 256

    let marks, _, _ =
        Seq.replicate rounds lengths
        |> Seq.concat
        |> Seq.fold step (Array.init 256 id, 0, 0)

    marks

/// Hashes ASCII text, including empty text, without trimming, into 16 bytes in block order.
/// Appends the standard suffix, runs 64 rounds and XORs consecutive 16-mark blocks.
/// The caller must supply ASCII text; non-ASCII text is outside the contract. Valid text cannot fail.
let dense (text: string) =
    let lengths = [ for c in text -> int c ] @ [ 17; 31; 73; 47; 23 ]

    lengths
    |> sparse 64
    |> Array.chunkBySize 16
    |> Array.map (Array.fold (^^^) 0 >> byte)
