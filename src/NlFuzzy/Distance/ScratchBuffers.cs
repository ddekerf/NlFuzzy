using System;

namespace NlFuzzy.Distance;

/// <summary>
/// Per-thread scratch arrays for the distance algorithms, so that scoring millions of pairs does
/// not allocate two short-lived arrays per call. Each thread owns its buffers, so the algorithms
/// stay thread-safe. Requests above <see cref="MaxPooledLength"/> get a fresh array instead, so a
/// single very long input does not pin a large buffer to the thread.
/// </summary>
internal static class ScratchBuffers
{
    internal const int MaxPooledLength = 512;

    [ThreadStatic]
    private static bool[]?[]? _flags;

    [ThreadStatic]
    private static int[]?[]? _ints;

    /// <summary>
    /// Returns a bool array of at least <paramref name="length"/> elements whose first
    /// <paramref name="length"/> elements are false. Slots 0 and 1 are independent.
    /// </summary>
    internal static bool[] Flags(int slot, int length)
    {
        if (length > MaxPooledLength)
        {
            return new bool[length];
        }

        var slots = _flags ??= new bool[2][];
        var buffer = slots[slot];

        if (buffer is null || buffer.Length < length)
        {
            buffer = new bool[Math.Max(length, 32)];
            slots[slot] = buffer;
            return buffer;
        }

        Array.Clear(buffer, 0, length);

        return buffer;
    }

    /// <summary>
    /// Returns an int array of at least <paramref name="length"/> elements with unspecified
    /// contents; callers must write every element before they read it. Slots 0 and 1 are
    /// independent.
    /// </summary>
    internal static int[] Ints(int slot, int length)
    {
        if (length > MaxPooledLength)
        {
            return new int[length];
        }

        var slots = _ints ??= new int[2][];
        var buffer = slots[slot];

        if (buffer is null || buffer.Length < length)
        {
            buffer = new int[Math.Max(length, 32)];
            slots[slot] = buffer;
        }

        return buffer;
    }
}
