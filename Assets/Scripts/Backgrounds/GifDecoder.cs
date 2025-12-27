using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TapOrb.Backgrounds
{
    /// <summary>
    /// Lightweight GIF decoder that reads frames and delays for runtime playback.
    /// Inspired by B83.Image.GIF implementation (MIT).
    /// </summary>
    public static class GifDecoder
    {
        private class GifImageDescriptor
        {
            public int Width;
            public int Height;
            public bool HasLocalColorTable;
            public bool Interlaced;
            public Color32[] LocalColorTable;
            public int TransparencyIndex = -1;
            public int DelayInHundredths;
        }

        public static List<GifFrame> Decode(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;

            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                ReadHeader(reader);
                var globalColorTable = ReadLogicalScreenDescriptor(reader, out int width, out int height);

                var frames = new List<GifFrame>();
                GifImageDescriptor currentDescriptor = null;

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    int blockId = reader.ReadByte();
                    switch (blockId)
                    {
                        case 0x21: // extension
                            int label = reader.ReadByte();
                            if (label == 0xF9) // graphic control extension
                            {
                                currentDescriptor = currentDescriptor ?? new GifImageDescriptor();
                                ReadGraphicControlExtension(reader, currentDescriptor);
                            }
                            else
                            {
                                SkipDataBlocks(reader);
                            }
                            break;
                        case 0x2C: // image descriptor
                            currentDescriptor = currentDescriptor ?? new GifImageDescriptor();
                            ReadImageDescriptor(reader, currentDescriptor, globalColorTable, width, height, frames);
                            currentDescriptor = null;
                            break;
                        case 0x3B: // trailer
                            reader.BaseStream.Position = reader.BaseStream.Length;
                            break;
                        default:
                            throw new Exception($"Unknown block in GIF: 0x{blockId:X2}");
                    }
                }

                return frames;
            }
        }

        private static void ReadHeader(BinaryReader reader)
        {
            var signature = new string(reader.ReadChars(3));
            var version = new string(reader.ReadChars(3));
            if (signature != "GIF")
                throw new Exception("Invalid GIF signature");
            if (version != "87a" && version != "89a")
                throw new Exception("Unsupported GIF version");
        }

        private static Color32[] ReadLogicalScreenDescriptor(BinaryReader reader, out int width, out int height)
        {
            width = reader.ReadUInt16();
            height = reader.ReadUInt16();
            byte packed = reader.ReadByte();
            bool globalColorTableFlag = (packed & 0x80) != 0;
            int globalColorTableSize = 2 << (packed & 0x07);
            reader.ReadByte(); // background color index (unused)
            reader.ReadByte(); // pixel aspect ratio (unused)

            if (globalColorTableFlag)
                return ReadColorTable(reader, globalColorTableSize);

            return null;
        }

        private static void ReadGraphicControlExtension(BinaryReader reader, GifImageDescriptor descriptor)
        {
            reader.ReadByte(); // block size
            byte packed = reader.ReadByte();
            descriptor.DelayInHundredths = reader.ReadUInt16();
            descriptor.TransparencyIndex = reader.ReadByte();
            reader.ReadByte(); // block terminator

            bool transparencyFlag = (packed & 0x01) != 0;
            if (!transparencyFlag)
                descriptor.TransparencyIndex = -1;
        }

        private static void ReadImageDescriptor(BinaryReader reader, GifImageDescriptor descriptor, Color32[] globalColorTable, int canvasWidth, int canvasHeight, List<GifFrame> frames)
        {
            int left = reader.ReadUInt16();
            int top = reader.ReadUInt16();
            descriptor.Width = reader.ReadUInt16();
            descriptor.Height = reader.ReadUInt16();

            byte packed = reader.ReadByte();
            descriptor.HasLocalColorTable = (packed & 0x80) != 0;
            descriptor.Interlaced = (packed & 0x40) != 0;
            int localColorTableSize = descriptor.HasLocalColorTable ? 2 << (packed & 0x07) : 0;

            var colorTable = descriptor.HasLocalColorTable ? ReadColorTable(reader, localColorTableSize) : globalColorTable;
            if (colorTable == null)
                throw new Exception("GIF is missing a color table");

            var lzwMinimumCodeSize = reader.ReadByte();
            var imageData = ReadImageData(reader);
            var pixels = DecodeImageData(imageData, lzwMinimumCodeSize, descriptor.Width, descriptor.Height);
            var texture = new Texture2D(canvasWidth, canvasHeight, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var colors = new Color32[canvasWidth * canvasHeight];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = new Color32(0, 0, 0, 0);

            for (int y = 0; y < descriptor.Height; y++)
            {
                int targetY = descriptor.Interlaced ? GetInterlacedRow(y, descriptor.Height) : y;
                for (int x = 0; x < descriptor.Width; x++)
                {
                    byte paletteIndex = pixels[y * descriptor.Width + x];
                    if (paletteIndex == descriptor.TransparencyIndex)
                        continue;
                    colors[(top + targetY) * canvasWidth + (left + x)] = colorTable[paletteIndex];
                }
            }

            texture.SetPixels32(colors);
            texture.Apply();

            float delay = Mathf.Max(0.02f, descriptor.DelayInHundredths / 100f);
            frames.Add(new GifFrame(texture, delay));
        }

        private static byte[] ReadImageData(BinaryReader reader)
        {
            using (var ms = new MemoryStream())
            {
                int blockSize = reader.ReadByte();
                while (blockSize > 0)
                {
                    ms.Write(reader.ReadBytes(blockSize), 0, blockSize);
                    blockSize = reader.ReadByte();
                }
                return ms.ToArray();
            }
        }

        private static byte[] DecodeImageData(byte[] data, int lzwMinimumCodeSize, int width, int height)
        {
            var output = new List<byte>(width * height);

            int clearCode = 1 << lzwMinimumCodeSize;
            int endCode = clearCode + 1;
            int codeSize = lzwMinimumCodeSize + 1;
            int dictionaryCapacity = 4096;
            var dictionary = new List<List<byte>>(dictionaryCapacity);

            void ResetDictionary()
            {
                dictionary.Clear();
                for (int i = 0; i < clearCode; i++)
                    dictionary.Add(new List<byte> { (byte)i });
                dictionary.Add(new List<byte>()); // clear
                dictionary.Add(new List<byte>()); // end
                codeSize = lzwMinimumCodeSize + 1;
            }

            ResetDictionary();

            int dataIndex = 0;
            int bitPosition = 0;
            int previousCode = -1;

            while (dataIndex < data.Length && output.Count < width * height)
            {
                int currentCode = ReadCode(data, ref dataIndex, ref bitPosition, codeSize);
                if (currentCode == clearCode)
                {
                    ResetDictionary();
                    previousCode = -1;
                    continue;
                }
                if (currentCode == endCode)
                    break;

                List<byte> entry;
                if (currentCode < dictionary.Count)
                {
                    entry = dictionary[currentCode];
                }
                else if (currentCode == dictionary.Count && previousCode != -1)
                {
                    var previousEntry = dictionary[previousCode];
                    entry = new List<byte>(previousEntry) { previousEntry[0] };
                }
                else
                {
                    break;
                }

                output.AddRange(entry);

                if (previousCode != -1)
                {
                    var previousEntry = dictionary[previousCode];
                    var newEntry = new List<byte>(previousEntry) { entry[0] };
                    if (dictionary.Count < dictionaryCapacity)
                        dictionary.Add(newEntry);

                    if (dictionary.Count == (1 << codeSize) && codeSize < 12)
                        codeSize++;
                }

                previousCode = currentCode;
            }

            // Ensure buffer length
            if (output.Count < width * height)
            {
                var missing = width * height - output.Count;
                for (int i = 0; i < missing; i++)
                    output.Add(0);
            }

            return output.ToArray();
        }

        private static int ReadCode(byte[] data, ref int index, ref int bitPosition, int codeSize)
        {
            int raw = 0;
            int bitsRead = 0;
            int byteIndex = index;
            int localBitPos = bitPosition;

            while (bitsRead < codeSize)
            {
                if (byteIndex >= data.Length)
                    break;

                int availableBits = 8 - localBitPos;
                int bitsToRead = Mathf.Min(codeSize - bitsRead, availableBits);
                int mask = (1 << bitsToRead) - 1;
                raw |= ((data[byteIndex] >> localBitPos) & mask) << bitsRead;

                bitsRead += bitsToRead;
                localBitPos += bitsToRead;

                if (localBitPos >= 8)
                {
                    localBitPos = 0;
                    byteIndex++;
                }
            }

            index = byteIndex;
            bitPosition = localBitPos;
            return raw;
        }

        private static Color32[] ReadColorTable(BinaryReader reader, int size)
        {
            var colors = new Color32[size];
            for (int i = 0; i < size; i++)
            {
                byte r = reader.ReadByte();
                byte g = reader.ReadByte();
                byte b = reader.ReadByte();
                colors[i] = new Color32(r, g, b, 255);
            }
            return colors;
        }

        private static void SkipDataBlocks(BinaryReader reader)
        {
            int blockSize = reader.ReadByte();
            while (blockSize > 0)
            {
                reader.BaseStream.Position += blockSize;
                blockSize = reader.ReadByte();
            }
        }

        private static int GetInterlacedRow(int row, int height)
        {
            // Interlace pattern: 0,8,16... then 4,12,... then 2,6,10,... then 1,3,5,...
            int[] offsets = { 0, 4, 2, 1 };
            int[] steps = { 8, 8, 4, 2 };

            int pass = 0;
            int currentRow = row;
            int accumulated = 0;

            while (pass < 4)
            {
                if (currentRow < ((height - offsets[pass] + steps[pass] - 1) / steps[pass]))
                    return offsets[pass] + currentRow * steps[pass];

                currentRow -= (height - offsets[pass] + steps[pass] - 1) / steps[pass];
                pass++;
            }

            return row;
        }
    }
}
