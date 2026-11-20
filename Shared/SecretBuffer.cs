using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace QrCodeGenerator
{
    /// <summary>
    /// Fixed-capacity, pinned character buffer for secrets. Never reallocated, never converted to a string,
    /// and zeroed on <see cref="Dispose"/>.
    /// </summary>
    internal sealed class SecretBuffer : IDisposable
    {
        public const int DefaultCapacity = 1024;

        private readonly char[] _chars;

        public SecretBuffer(int capacity = DefaultCapacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
            _chars = GC.AllocateArray<char>(capacity, pinned: true);
        }

        public int Length { get; private set; }

        public int Capacity => _chars.Length;

        public bool IsEmpty => Length == 0;

        public ReadOnlySpan<char> Span => _chars.AsSpan(0, Length);

        /// <summary>Whole backing store, for tests that check erasure.</summary>
        internal ReadOnlySpan<char> Backing => _chars;

        /// <summary>Appends a character; returns false (and drops it) when the buffer is full.</summary>
        public bool Append(char c)
        {
            if (Length >= _chars.Length)
            {
                return false;
            }

            _chars[Length++] = c;
            return true;
        }

        public bool RemoveLast()
        {
            if (Length == 0)
            {
                return false;
            }

            _chars[--Length] = '\0';
            return true;
        }

        /// <summary>Clears the buffer and returns its whole storage for a single direct write; follow with <see cref="SetLength"/>.</summary>
        internal Span<char> ResetForWrite()
        {
            Clear();
            return _chars;
        }

        internal void SetLength(int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(length, _chars.Length);
            Length = length;
        }

        public void Clear()
        {
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(_chars.AsSpan()));
            Length = 0;
        }

        public void Dispose() => Clear();
    }
}
