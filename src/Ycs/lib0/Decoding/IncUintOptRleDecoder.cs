﻿// ------------------------------------------------------------------------------
//  <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
// ------------------------------------------------------------------------------

namespace Ycs
{
    /// <seealso cref="IncUintOptRleEncoder"/>
    internal class IncUintOptRleDecoder : AbstractStreamDecoder<uint>
    {
        private uint _state;
        private uint _count;

        public IncUintOptRleDecoder(Stream input, bool leaveOpen = false)
            : base(input, leaveOpen)
        {
            // Do nothing.
        }

        /// <inheritdoc/>
        public override uint Read()
        {
            CheckDisposed();

            if (_count == 0)
            {
                var (value, sign) = Stream.ReadVarInt();

                // If the sign is negative, we read the count too; otherwise. count is 1.
                bool isNegative = sign < 0;
                if (isNegative)
                {
                    _state = (uint)(-value);
                    _count = Stream.ReadVarUint() + 2;
                }
                else
                {
                    _state = (uint)value;
                    _count = 1;
                }
            }

            _count--;
            return _state++;
        }
    }
}
