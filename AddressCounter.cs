using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace FastMassPing
{
    internal class AddressCounter : IEnumerable<IPAddress>
    {
        private readonly IPAddress startAddress;
        private readonly IPAddress endAddress;
        private readonly long addressSpace;

        public IPAddress FirstAddress => startAddress;
        public IPAddress LastAddress => endAddress;
        public long AddressSpace => addressSpace;

        public AddressCounter(IPAddress startAddress, IPAddress endAddress)
        {
            this.startAddress = startAddress;
            this.endAddress = endAddress;
            addressSpace = GetAddressSpace(startAddress, endAddress);
        }

        public IEnumerator<IPAddress> GetEnumerator()
        {
            return new AddressEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static IPAddress IncrementAddress(IPAddress address, long increment)
        {
            byte[] addressBytes = address.GetAddressBytes();
            Array.Reverse(addressBytes);
            addressBytes = BitConverter.GetBytes(BitConverter.ToInt32(addressBytes, 0) + increment);
            Array.Reverse(addressBytes);
            return new IPAddress(addressBytes);
        }

        public static long GetAddressSpace(IPAddress address1, IPAddress address2)
        {
            byte[] address1Bytes = address1.GetAddressBytes();
            byte[] address2Bytes = address2.GetAddressBytes();
            Array.Reverse(address1Bytes);
            Array.Reverse(address2Bytes);
            return BitConverter.ToUInt32(address2Bytes, 0) - BitConverter.ToUInt32(address1Bytes, 0);
        }
    }

    internal class AddressEnumerator : IEnumerator<IPAddress>
    {
        private readonly AddressCounter addressCounter;
        private IPAddress currentAddress;
        private long currentPosition;

        public AddressEnumerator(AddressCounter addressCounter)
        {
            this.addressCounter = addressCounter;
            Reset();
        }

        public IPAddress Current => currentAddress;

        object IEnumerator.Current => currentAddress;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            currentAddress = AddressCounter.IncrementAddress(currentAddress, 1);
            currentPosition++;
            return currentPosition < addressCounter.AddressSpace - 1;
        }

        public void Reset()
        {
            currentAddress = addressCounter.FirstAddress;
            currentPosition = 0;
        }
    }
}
