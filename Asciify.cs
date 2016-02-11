/*
The MIT License (MIT)

Copyright (c) 2016 AndrewNeudegg, https://github.com/AndrewNeudegg

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AsciiArt
{
    public class ASCIIfy
    {
        #region Properties

        private static Bitmap _asciiBitmap;
        private static string _asciiString;
        private static Bitmap _preTransformBitmap;

        /// <summary>
        ///     Gets or sets the type face to use.
        /// </summary>
        public Font TypeFaceFont { get; set; }

        /// <summary>
        ///     Gets or sets the brush to use.
        /// </summary>
        public Color TypeFaceColour { get; set; }

        public Rectangle PixelSizeRectangle { get; set; }

        public double OutputSquareScale { get; set; }

        /// <summary>
        ///     Gets the converted ASCII Bitmap. ReadOnly.
        /// </summary>
        public Bitmap AsciiBitmap
        {
            get
            {
                if (_asciiBitmap == null)
                {
                    _asciiBitmap = GetAsciiBitmap(_preTransformBitmap, PixelSizeRectangle, TypeFaceFont, TypeFaceColour,
                        OutputSquareScale);
                }
                return _asciiBitmap;
            }
        }

        /// <summary>
        ///     Gets the created ASCII string.
        /// </summary>
        public string AsciiString
        {
            get
            {
                if (_asciiString == null)
                {
                    _asciiString = GetAsciiString(_preTransformBitmap, PixelSizeRectangle, OutputSquareScale);
                }
                return _asciiString;
            }
        }

        #endregion

        #region Initialisers

        /// <summary>
        ///     Allows the new keyword to be used as an accessor.
        /// </summary>
        /// <param name="font">The Font to use in rendering the image.</param>
        /// <param name="brush">The brush to use in drawing the text.</param>
        public ASCIIfy(Bitmap bitmap, Font font, Color colour, Rectangle pixelSizeRectangle, double outputScale)
        {
            _preTransformBitmap = bitmap;
            TypeFaceFont = font;
            TypeFaceColour = colour;
            PixelSizeRectangle = pixelSizeRectangle;
            OutputSquareScale = outputScale;
        }

        /// <summary>
        ///     Standard initialisation.
        /// </summary>
        public ASCIIfy()
        {
        }

        #endregion

        #region Operator Overloads

        /// <summary>
        ///     Implicit conversion from this class to an ASCIIfied bitmap.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static implicit operator Bitmap(ASCIIfy x)
        {
            if (_asciiBitmap == null)
            {
                throw new Exception("ASCIIFY has not been initialised.");
            }
            return x.AsciiBitmap;
        }

        /// <summary>
        ///     Implicit conversion from this class to an ASCIIfied string.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static implicit operator string(ASCIIfy x)
        {
            if (_asciiBitmap == null)
            {
                throw new Exception("ASCIIFY has not been initialised.");
            }
            return x.AsciiString;
        }

        #endregion

        #region Structures

        /// <summary>
        ///     Contains information for one pixel for faster lookups.
        /// </summary>
        private struct Pixel
        {
            public byte R;
            public byte B;
            public byte G;
            public readonly byte A;

            public Pixel(byte r, byte b, byte g, byte a)
            {
                R = r;
                B = b;
                G = g;
                A = a;
            }
        }

        /// <summary>
        ///     Defines a section of the pixel matrix with an averaged colour.
        /// </summary>
        private struct Region
        {
            public Rectangle OperableRectangle;
            public readonly Pixel IdentifyingPixel;

            public Region(Rectangle rectangle, Pixel pixel)
            {
                OperableRectangle = rectangle;
                IdentifyingPixel = pixel;
            }
        }

        #endregion

        #region SupervisoryCode

        /// <summary>
        ///     Generates an ASCII Bitmap.
        /// </summary>
        /// <param name="bitmap">The bitmap to convert.</param>
        /// <param name="BlockSizeX">The Size of the divisions X.</param>
        /// <param name="BlockSizeY">The size of the divisions Y.</param>
        /// <param name="font">The font to use.</param>
        /// <param name="brush">The brush to use.</param>
        /// <returns>Bitmap of ASCII text rendered on white background.</returns>
        public Bitmap GetAsciiBitmap(Bitmap bitmap, Rectangle pixelSizeRectangle, Font font, Color color,
            double sizeRatio)
        {
            return ResizeBitmap(generateBitmap(bitmap.Width, bitmap.Height,
                GenerateAsciiStringList(GenerateRegionsList(bitmapToMatrixList(bitmap), pixelSizeRectangle)), font,
                color), sizeRatio);
        }

        /// <summary>
        ///     Gets the associated ASCII string from a bitmap.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="BlockSizeX"></param>
        /// <param name="BlockSizeY"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public string GetAsciiString(Bitmap bitmap, Rectangle pixelSizeRectangle, double sizeRatio)
        {
            return
                ConcatenateAscii(
                    GenerateAsciiStringList(GenerateRegionsList(bitmapToMatrixList(ResizeBitmap(bitmap, sizeRatio)),
                        pixelSizeRectangle)));
        }

        #endregion

        #region Core Methods

        /// <summary>
        ///     Converts a bitmap into a matrix of pixels.
        /// </summary>
        /// <param name="bitmap">A bitmap file.</param>
        /// <returns>List[list][[pixel]];</returns>
        private List<List<Pixel>> bitmapToMatrixList(Bitmap bitmap)
        {
            var returnList = new List<List<Pixel>>(); // Create a temporary array to return
            for (int x = 0; x < bitmap.Height; x++) // Itterate over the X axis
            {
                var tempList = new List<Pixel>(); // Temporary list for containing variables.
                for (int y = 0; y < bitmap.Width; y++) // Itterate over the y axis.
                {
                    Color pixel = bitmap.GetPixel(y, x); // Get a copy of the pixel
                    var px = new Pixel(pixel.R, pixel.B, pixel.G, pixel.A); // Load pixel into pixel struct.
                    tempList.Add(px);
                }
                returnList.Add(tempList); // Add that row of pixels to list.
            }
            return returnList;
        }

        /// <summary>
        ///     Generates a List[Region]s that can be used to reillustrate the picture. Uses mean.
        /// </summary>
        /// <param name="pixelsList">The pixel matrix.</param>
        /// <param name="rectangle">Height and Width dimensions only.</param>
        /// <returns></returns>
        private List<List<Region>> GenerateRegionsList(List<List<Pixel>> pixelsList, Rectangle rectangle)
        {
            var returnList = new List<List<Region>>(); // Create a temporary variable array to return.

            int RegionHeight = rectangle.Height; // Tidying up some variables.
            int RegionWidth = rectangle.Width; // For readability.
            int RegionArea = RegionHeight*RegionWidth; // Total pixel area.

            for (int x = 0; x < pixelsList.Count/RegionWidth; x++)
            {
                int _regionX = x*RegionWidth;
                int _regionY = 0; // Holds the current Y value
                int _R = 0;
                int _B = 0;
                int _G = 0;
                int _A = 0;
                var tempList = new List<Region>();
                for (int y = 0; y < pixelsList[0].Count/RegionHeight; y++)
                    // Assumes a rectangular image.. Find an exception?
                {
                    _regionY = y*RegionHeight;

                    // Search within region.
                    for (int px = 0; px < RegionWidth; px++)
                    {
                        // Grab this pixels info.
                        Pixel pixel = pixelsList[_regionX + px][_regionY];
                        _R += pixel.R;
                        _B += pixel.B;
                        _G += pixel.G;
                        _A += pixel.A;
                    }
                    tempList.Add(new Region(new Rectangle(_regionX, _regionY, 0, 0),
                        new Pixel((byte) (_R/RegionArea), (byte) (_B/RegionArea), (byte) (_G/RegionArea),
                            (byte) (_A/RegionArea))));
                    _R = 0;
                    _B = 0;
                    _G = 0;
                    _A = 0;
                }
                returnList.Add(tempList);
                tempList = new List<Region>();
            }
            return returnList;
        }


        /// <summary>
        ///     Generates an ASCII string list with Environment.NewLine character appeneded if requested.
        /// </summary>
        /// <param name="regions">Regions to be evaluated.</param>
        /// <param name="addNewLine">Append new line character.</param>
        /// <returns>A List of ASCII strings.</returns>
        private List<string> GenerateAsciiStringList(List<List<Region>> regions, bool addNewLine = false)
        {
            var returnList = new List<string>(); // Declare temperary variable.
            var stringBuilder = new StringBuilder(); // String builder to construct strings.
            int currentY = 0;
            bool toggle = true;
            foreach (var regionList in regions) // Itterate over regions
            {
                foreach (Region region in regionList)
                {
                    // Get the character
                    stringBuilder.Append(GenerateCharFromPixel(region.IdentifyingPixel));
                }

                if (addNewLine)
                {
                    stringBuilder.Append(Environment.NewLine);
                }

                if (toggle) // Vertical Compression.
                {
                    returnList.Add(stringBuilder.ToString());
                    toggle = false;
                }
                else
                {
                    toggle = true;
                }

                stringBuilder.Clear();
            }
            returnList.Add(stringBuilder.ToString()); // Flush the stringbuilder
            return returnList;
        }

        /// <summary>
        ///     Converts a pixel colour into greyscale then into a character.
        /// </summary>
        /// <param name="pixel">A pixel structure.</param>
        /// <returns>A character most like that shade.</returns>
        private char GenerateCharFromPixel(Pixel pixel)
        {
            //char[] _AsciiChars = { '█', '░', '@','&','$', '%','!', '(',')', '=', '+','^', '*',';', ':','_', '-','"','/', ',', '.', ' ' }; // Simple Chars.
            char[] _AsciiChars = {'█', '░', '@', '%', '=', '+', '*', ':', '-', '.', ' '}; // Complex Chars.
            if (pixel.R == 0 & pixel.B == 0 & pixel.G == 0 & pixel.A == 0)
            {
                pixel.B = 255;
                pixel.G = 255;
                pixel.R = 255;
            }
            else if (pixel.A == 0)
            {
                return _AsciiChars[_AsciiChars.Count() - 1]; // The lightest ( since its transparent.
            }
            var grey = (int) ((0.299d*pixel.R + 0.587d*pixel.G + 0.114d*pixel.B));
            Color greyColor = Color.FromArgb(grey, grey, grey);
            return _AsciiChars[(greyColor.R*_AsciiChars.Count() - 1)/255];
        }

        /// <summary>
        ///     Concatenates an ascii list string.
        ///     DOES NOT REQUIRE THE NEW LINE FLAG.
        /// </summary>
        /// <param name="asciiList">The Ascii list.</param>
        /// <returns>An ASCII string.</returns>
        private string ConcatenateAscii(List<string> asciiList)
        {
            return string.Join(Environment.NewLine, asciiList.ToArray());
        }

        #endregion

        #region Image Functions

        /// <summary>
        ///     Renders ASCII drawn text to a bitmap.
        ///     DOES NOT REQUIRE ENVIRONEMENT.NEWLINE.
        /// </summary>
        /// <param name="width">Canvas Width.</param>
        /// <param name="height">Canvas Height.</param>
        /// <param name="stringsList">List of strings.</param>
        /// <param name="font">Font to render in.</param>
        /// <param name="brush">brush to render with.</param>
        /// <returns>Bitmap version of the text.</returns>
        private Bitmap generateBitmap(int width, int height, List<string> stringsList, Font font, Color color)
        {
            string text = string.Join(Environment.NewLine, stringsList.ToArray());
            Size size = TextRenderer.MeasureText(text, font);
            var returnBitmap = new Bitmap(size.Width + 10, size.Height + 10); // Create temporary variable to return.
            using (Graphics g = Graphics.FromImage(returnBitmap)) // Create new graphics object.
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.Clear(Color.Transparent);
                TextRenderer.DrawText(g, text, font, new Point(5, 5), color);
                //g.DrawString(text, font, brush, new PointF(5f,5f));
            }
            return returnBitmap;
        }

        /// <summary>
        ///     Resize a Bitmap
        /// </summary>
        /// <param name="inputBitmap"></param>
        /// <param name="asciiWidth"></param>
        /// <returns></returns>
        /// http://www.c-sharpcorner.com/UploadFile/dheenu27/ImageToASCIIconverter03022007164455PM/ImageToASCIIconverter.aspx
        private Bitmap ResizeBitmap(Bitmap inputBitmap, double factor)
        {
            //Calculate the new Height of the image from its width
            var asciiWidth = (int) Math.Ceiling(inputBitmap.Width*factor);
            var asciiHeight = (int) Math.Ceiling(inputBitmap.Height*factor);

            // If size is too big this will throw an exception
            //Create a new Bitmap and define its resolution
            var result = new Bitmap(asciiWidth, asciiHeight);
            Graphics g = Graphics.FromImage(result);
            //The interpolation mode produces high quality images
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(inputBitmap, 0, 0, asciiWidth, asciiHeight);
            g.Dispose();
            return result;
        }

        #endregion
    }
}
