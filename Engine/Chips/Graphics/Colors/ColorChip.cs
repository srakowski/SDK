﻿//  
// Copyright (c) Jesse Freeman. All rights reserved.  
// 
// Licensed under the Microsoft Public License (MS-PL) License. 
// See LICENSE file in the project root for full license information. 
// 
// Contributors
// --------------------------------------------------------
// This is the official list of Pixel Vision 8 contributors:
//  
// Jesse Freeman
// 

using System;
using System.Collections.Generic;
using System.Text;
using PixelVisionSDK.Engine.Chips.Data;
using PixelVisionSDK.Engine.Utils;

namespace PixelVisionSDK.Engine.Chips.Graphics.Colors
{
    /// <summary>
    ///     The <see cref="ColorChip" /> represents the system colors of the engine.
    ///     It allows the engine to work in color indexes that the display can map
    ///     to actual colors in Unity via the class's set of APIs.
    /// </summary>
    public class ColorChip : AbstractChip, IColorChip
    {
        protected string[] _colors =
        {
            "#000000",
            "#FFFFFF",
            "#9D9D9D",
            "#BE2633",
            "#E06F8B",
            "#493C2B",
            "#A46422",
            "#EB8931",
            "#F7E26B",
            "#2F484E",
            "#44891A",
            "#A3CE27",
            "#1B2632",
            "#005784",
            "#31A2F2",
            "#B2DCEF"
        };

        protected int _colorsPerPage = 64;
        protected int _pages = 4;
        protected string _transparent = "#FF00FF";
        protected ColorData[] colorCache;
        private bool invalid;
        protected Vector pageSize = new Vector(8, 8);

        /// <summary>
        ///     Defines the total number of colors per virtual page.
        /// </summary>
        /// <value>Int</value>
        public int colorsPerPage
        {
            get { return _colorsPerPage; }
        }

        /// <summary>
        ///     Returns the total virtual pages of colors.
        /// </summary>
        /// <value>Int</value>
        public int pages
        {
            get { return _pages; }
            set
            {
                if (_pages == value)
                    return;

                _pages = value.Clamp(1, 4);

                var oldTotal = _colors.Length;

                Array.Resize(ref _colors, total);

                if (oldTotal < total)
                {
                    for (var i = oldTotal; i < total; i++)
                    {
                        _colors[i] = transparent;
                    }
                }
            }
        }

        /// <summary>
        ///     The default <see cref="transparent" /> color to be used in the
        ///     engine.
        /// </summary>
        /// <value>String</value>
        public string transparent
        {
            get { return _transparent; }

            set { _transparent = value; }
        }

        /// <summary>
        ///     Get and Set the <see cref="supportedColors" /> number of <see cref="colors" />
        ///     in the palette. Changing the <see cref="supportedColors" /> will clear the
        ///     palette when it resizes.
        /// </summary>
        /// <value>Int</value>
        public int supportedColors
        {
            get
            {
                var count = 0;
                var total = _colors.Length;
                for (var i = 0; i < total; i++)
                {
                    if (_colors[i] != transparent)
                        count ++;
                }

                return count;
            }
        }

        /// <summary>
        ///     The <see cref="supportedColors" /> number of <see cref="colors" /> the chip can
        ///     support. This lock makes the sure that the <see cref="colors" />
        ///     array will never be larger than this value.
        /// </summary>
        /// <value>Int</value>
        public int total
        {
            get { return pages*colorsPerPage; }
        }

        /// <summary>
        ///     Returns a list of color data to be used for rendering.
        /// </summary>
        /// <value>ColorData[]</value>
        public ColorData[] colors
        {
            get
            {
                if (invalid)
                {
                    var t = total;
                    colorCache = new ColorData[t];

                    for (var i = 0; i < t; i++)
                    {
                        colorCache[i] = new ColorData(_colors[i]);
                        //color.HexToColor(_colors[i]);
                        //colorCache[i] = color;
                    }
                    invalid = false;
                }
                return colorCache;
            }
        }

        public string ReadColorAt(int index)
        {
            return index < 0 || index > _colors.Length - 1 ? transparent : _colors[index];
        }

        public int FindColorID(string color)
        {
            return Array.IndexOf(_colors, color);
        }

        public void Clear()
        {
            var t = _colors.Length;
            for (var i = 0; i < t; i++)
            {
                UpdateColorAt(i, transparent);
            }
        }

        public void UpdateColorAt(int index, string color)
        {
            if (index > _colors.Length)
                return;

            if (ColorData.ValidateColor(color))
            {
                _colors[index] = color;
            }

            invalid = true;
        }


        /// <summary>
        ///     This method configures the chip. It registers itself with the
        ///     engine as the default ColorChip, it sets the supported
        ///     <see cref="colors" /> to the maximum value of 256 and calls
        ///     <see cref="RevertColors" /> to add the default <see cref="colors" />
        ///     to the <see cref="colors" /> array.
        /// </summary>
        public override void Configure()
        {
            engine.colorChip = this;
            RebuildColorPages(16);
        }

        public override void Deactivate()
        {
            base.Deactivate();
            engine.colorChip = null;
        }

        /// <summary>
        ///     Recalculates the total number of pages based on the new total
        ///     number of colors.
        /// </summary>
        /// <param name="total"></param>
        public void RebuildColorPages(int total)
        {
            pages = MathUtil.CeilToInt(total/colorsPerPage);
        }

        /// <summary>
        ///     This method converts the chip's supportedColors and defaultColors into a json object.
        /// </summary>
        /// <param name="sb"></param>
        public override void CustomSerializedData(StringBuilder sb)
        {
            sb.Append("\"pages\":");
            sb.Append(pages);
            sb.Append(",");

            sb.Append("\"colorsPerPage\":");
            sb.Append(colorsPerPage);
            sb.Append(",");

            sb.Append("\"transparent\":\"");
            sb.Append(transparent);
            sb.Append("\",");

            // Default Colors
            sb.Append("\"colors\":[");

            var total = _colors.Length;
            for (var i = 0; i < total; i++)
            {
                var color = _colors[i];
                if (color != _transparent)
                {
                    sb.Append("\"");
                    sb.Append(color);
                    sb.Append("\"");
                    sb.Append(",");
                }
            }

            if (sb.ToString().EndsWith(","))
                sb.Length -= 1;

            sb.Append("]");
        }

        /// <summary>
        ///     This method reads the supplied Dictionary and attempts to set the supportedColors value and rebuild the
        ///     defaultColors array. Once done, it calls RevertColors().
        /// </summary>
        /// <param name="data"></param>
        public override void DeserializeData(Dictionary<string, object> data)
        {
            if (data.ContainsKey("colorsPerPage"))
            {
                _colorsPerPage = (int) (long) data["colrosPerPage"];
            }

            if (data.ContainsKey("transparent"))
            {
                _transparent = (string) data["transparent"];
            }

            // Make sure we have data to parse
            if (data.ContainsKey("colors"))
            {
                // Pull out the color data
                var colors = (List<object>) data["colors"];

                var newTotal = colors.Count;
                RebuildColorPages(newTotal);
                Clear();
                for (var i = 0; i < newTotal; i++)
                {
                    UpdateColorAt(i, (string) colors[i]);
                }
            }
        }
    }
}