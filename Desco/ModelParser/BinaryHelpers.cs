using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Cobalt.IO;

namespace Desco.ModelParser
{
    public static class BinaryHelpers
    {
        /* http://stackoverflow.com/a/13651917 */
        static object lockObj = new object();
        static IDictionary<Type, Func<EndianBinaryReader, dynamic>> readers = null;

        static BinaryHelpers()
        {
            lock (lockObj)
            {
                if (readers == null)
                {
                    readers = new Dictionary<Type, Func<EndianBinaryReader, dynamic>>();
                    readers.Add(typeof(char), r => r.ReadChar());
                    readers.Add(typeof(byte), r => r.ReadByte());
                    readers.Add(typeof(short), r => r.ReadInt16());
                    readers.Add(typeof(ushort), r => r.ReadUInt16());
                    readers.Add(typeof(int), r => r.ReadInt32());
                    readers.Add(typeof(uint), r => r.ReadUInt32());
                    readers.Add(typeof(long), r => r.ReadInt64());
                    readers.Add(typeof(ulong), r => r.ReadUInt64());
                }
            }
        }

        public static Dictionary<TKey, ICollection<TValue>> GetMultiDictionary<TKey, TValue>(Stream stream, uint pointer)
        {
            if (!readers.ContainsKey(typeof(TKey))) throw new ArgumentException("Unsupported key type");
            if (!readers.ContainsKey(typeof(TValue))) throw new ArgumentException("Unsupported value type");

            int keySize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(TKey));
            int valueSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(TValue));

            long lastPosition = stream.Position;
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);
            stream.Seek(pointer, SeekOrigin.Begin);

            int numPairs = reader.ReadInt32();
            Dictionary<TKey, ICollection<TValue>> dictionary = new Dictionary<TKey, ICollection<TValue>>(numPairs);
            for (int i = 0; i < numPairs; i++)
            {
                stream.Seek((pointer + 0x04) + (i * (keySize + valueSize)), SeekOrigin.Begin);

                TKey key = readers[typeof(TKey)](reader);
                TValue value = readers[typeof(TValue)](reader);

                if (!dictionary.ContainsKey(key)) dictionary.Add(key, new List<TValue>());
                if (!dictionary[key].Contains(value)) dictionary[key].Add(value);
            }

            stream.Position = lastPosition;
            return dictionary;
        }

        public static T[] GetArray<T>(Stream stream, uint pointer) where T : ParsableData, IComponent
        {
            long lastPosition = stream.Position;
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);
            stream.Seek(pointer, SeekOrigin.Begin);

            T[] elements = new T[reader.ReadUInt32()];
            for (int i = 0; i < elements.Length; i++)
            {
                stream.Seek((pointer + 0x04) + (i * 0x04), SeekOrigin.Begin);
                stream.Seek(pointer + reader.ReadUInt32(), SeekOrigin.Begin);
                elements[i] = (T)Activator.CreateInstance(typeof(T), new object[] { stream });
            }

            stream.Position = lastPosition;
            return elements;
        }
    }
}
