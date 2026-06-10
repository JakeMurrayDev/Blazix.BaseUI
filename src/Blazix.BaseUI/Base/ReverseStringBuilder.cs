using System.Buffers;
using System.Globalization;

namespace Blazix.BaseUI.Base;

internal ref struct ReverseStringBuilder
{
    public const int MinimumRentedArraySize = 1024;

    private static readonly ArrayPool<char> ArrayPool = ArrayPool<char>.Shared;

    private int nextEndIndex;
    private Span<char> currentBuffer;
    private SequenceSegment? fallbackSequenceSegment;

    // For testing.
    internal readonly int SequenceSegmentCount => fallbackSequenceSegment?.Count() ?? 0;

    public ReverseStringBuilder(int conservativeEstimatedStringLength)
    {
        var array = ArrayPool.Rent(conservativeEstimatedStringLength);
        fallbackSequenceSegment = new(array);
        currentBuffer = array;
        nextEndIndex = currentBuffer.Length;
    }

    public ReverseStringBuilder(Span<char> initialBuffer)
    {
        currentBuffer = initialBuffer;
        nextEndIndex = currentBuffer.Length;
    }

    public readonly bool Empty => nextEndIndex == currentBuffer.Length;

    public void InsertFront(scoped ReadOnlySpan<char> span)
    {
        var startIndex = nextEndIndex - span.Length;
        if (startIndex >= 0)
        {
            // The common case. There is enough space in the current buffer to copy the given span.
            // No additional work needs to be done here after the copy.
            span.CopyTo(currentBuffer[startIndex..]);
            nextEndIndex = startIndex;
            return;
        }

        // There wasn't enough space in the current buffer.
        // What we do next depends on whether we're writing to the provided "initial" buffer or a rented one.

        if (fallbackSequenceSegment is null)
        {
            // We've been writing to a stack-allocated buffer, but there is no more room on the stack.
            // We rent new memory with a length sufficiently larger than the initial buffer
            // and copy the contents over.
            var remainingLength = -startIndex;
            var sizeToRent = currentBuffer.Length + Math.Max(MinimumRentedArraySize, remainingLength * 2);
            var newBuffer = ArrayPool.Rent(sizeToRent);
            fallbackSequenceSegment = new(newBuffer);

            var newEndIndex = newBuffer.Length - currentBuffer.Length + nextEndIndex;
            currentBuffer[nextEndIndex..].CopyTo(newBuffer.AsSpan(newEndIndex));
            newEndIndex -= span.Length;
            span.CopyTo(newBuffer.AsSpan(newEndIndex));

            currentBuffer = newBuffer;
            nextEndIndex = newEndIndex;
        }
        else
        {
            // We can't fit the whole string in the current heap-allocated buffer.
            // Copy as much as we can to the current buffer, rent a new buffer, and
            // continue copying the remaining contents.
            var remainingLength = -startIndex;
            span[remainingLength..].CopyTo(currentBuffer);
            span = span[..remainingLength];

            var sizeToRent = Math.Max(MinimumRentedArraySize, remainingLength * 2);
            var newBuffer = ArrayPool.Rent(sizeToRent);
            fallbackSequenceSegment = new(newBuffer, fallbackSequenceSegment);
            currentBuffer = newBuffer;

            startIndex = currentBuffer.Length - remainingLength;
            span.CopyTo(currentBuffer[startIndex..]);
            nextEndIndex = startIndex;
        }
    }

    public void InsertFront<T>(T value) where T : ISpanFormattable
    {
        // This is large enough for any integer Value (10 digits plus the possible sign).
        // We won't try to optimize for anything larger.
        Span<char> result = stackalloc char[11];

        if (value.TryFormat(result, out var charsWritten, format: default, CultureInfo.InvariantCulture))
        {
            InsertFront(result[..charsWritten]);
        }
        else
        {
            InsertFront((IFormattable)value);
        }
    }

    public void InsertFront(IFormattable formattable)
        => InsertFront(formattable.ToString(null, CultureInfo.InvariantCulture));

    public override readonly string ToString()
        => fallbackSequenceSegment is null
            ? new(currentBuffer[nextEndIndex..])
            : fallbackSequenceSegment.ToString(nextEndIndex);

    public readonly void Dispose()
    {
        fallbackSequenceSegment?.Dispose();
    }

    private sealed class SequenceSegment : ReadOnlySequenceSegment<char>, IDisposable
    {
        private readonly char[] array;

        public SequenceSegment(char[] array, SequenceSegment? next = null)
        {
            this.array = array;
            Memory = array;
            Next = next;
        }

        // For testing.
        internal int Count()
        {
            var count = 0;
            for (var current = this; current is not null; current = current.Next as SequenceSegment)
            {
                count++;
            }
            return count;
        }

        public string ToString(int startIndex)
        {
            RunningIndex = 0;

            var tail = this;
            while (tail.Next is SequenceSegment next)
            {
                next.RunningIndex = tail.RunningIndex + tail.Memory.Length;
                tail = next;
            }

            var sequence = new ReadOnlySequence<char>(this, startIndex, tail, tail.Memory.Length);
            return sequence.ToString();
        }

        public void Dispose()
        {
            for (var current = this; current is not null; current = current.Next as SequenceSegment)
            {
                ArrayPool.Return(current.array);
            }
        }
    }
}