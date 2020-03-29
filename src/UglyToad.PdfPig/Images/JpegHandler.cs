﻿namespace UglyToad.PdfPig.Images
{
    using System;
    using System.IO;

    internal static class JpegHandler
    {
        private const byte MarkerStart = 255;
        private const byte StartOfImage = 216;

        public static JpegInformation GetInformation(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!HasRecognizedHeader(stream))
            {
                throw new InvalidOperationException("The input stream did not start with the expected JPEG header [ 255 216 ]");
            }

            var marker = JpegMarker.StartOfImage;

            var shortBuffer = new byte[2];

            while (marker != JpegMarker.EndOfImage)
            {
                switch (marker)
                {
                    case JpegMarker.StartOfBaselineDctFrame:
                    {
                        // ReSharper disable once UnusedVariable
                        var length = ReadShort(stream, shortBuffer);
                        var bpp = stream.ReadByte();
                        var height = ReadShort(stream, shortBuffer);
                        var width = ReadShort(stream, shortBuffer);

                        return new JpegInformation(width, height, bpp);
                    }
                    case JpegMarker.StartOfProgressiveDctFrame:
                        break;
                }

                marker = (JpegMarker)ReadSegmentMarker(stream, true);
            }

            throw new InvalidOperationException("File was a valid JPEG but the width and height could not be determined.");
        }

        private static bool HasRecognizedHeader(Stream stream)
        {
            var bytes = new byte[2];

            var read = stream.Read(bytes, 0, 2);

            if (read != 2)
            {
                return false;
            }

            return bytes[0] == MarkerStart
                   && bytes[1] == StartOfImage;
        }

        private static byte ReadSegmentMarker(Stream stream, bool skipData = false)
        {
            byte? previous = null;
            int currentValue;
            while ((currentValue = stream.ReadByte()) != -1)
            {
                var b = (byte)currentValue;

                if (!skipData)
                {
                    if (!previous.HasValue && b != MarkerStart)
                    {
                        throw new InvalidOperationException();
                    }

                    if (b != MarkerStart)
                    {
                        return b;
                    }
                }

                if (previous.HasValue && previous.Value == MarkerStart && b != MarkerStart)
                {
                    return b;
                }

                previous = b;
            }

            throw new InvalidOperationException();
        }

        private static short ReadShort(Stream stream, byte[] buffer)
        {
            var read = stream.Read(buffer, 0, 2);

            if (read != 2)
            {
                throw new InvalidOperationException("Failed to read a short where expected in the JPEG stream.");
            }

            return (short) ((buffer[0] << 8) + buffer[1]);
        }
    }
}