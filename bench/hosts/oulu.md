# oulu

OS: NixOS 26.05 (Yarara) X64
CPU: 13th Gen Intel(R) Core(TM) i7-1355U
Compiler: SDK 10.0.401; runtime .NET 10.0.12
Language: F# 10

Work counts user-space instructions across runtime/JIT and solver; paired startup subtraction retains noise.
Pinned to allowed CPU 0.
Wall microseconds include process launch, redirected IO, GNU time and .NET runtime/JIT startup; no baseline subtraction.
CPU microseconds come from GNU time user+system, rounded to 10000 us per component; small samples are coarse.
RSS KiB is GNU time maximum resident set per fresh process, peak across timing runs, including runtime.
Variants are interleaved per phase; work uses 5 full/empty pairs when available. Timing uses 10 runs, or 3 after a pilot exceeds 1 s.
Background system load and runtime/JIT nondeterminism remain; no generated-code mechanism or speedup is inferred.
