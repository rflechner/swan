﻿namespace Unosquare.Swan
{
    using System;
    using Abstractions;

    /// <summary>
    /// Represents an atomically readabl;e or writable integer.
    /// </summary>
    public class AtomicInteger : AtomicTypeBase<int>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AtomicInteger"/> class.
        /// </summary>
        /// <param name="initialValue">if set to <c>true</c> [initial value].</param>
        public AtomicInteger(int initialValue = default)
            : base(Convert.ToInt64(initialValue))
        {
            // placeholder
        }

        /// <inheritdoc/>
        protected override int FromLong(long backingValue) =>
            Convert.ToInt32(backingValue);

        /// <inheritdoc/>
        protected override long ToLong(int value) =>
            Convert.ToInt64(value);
    }
}