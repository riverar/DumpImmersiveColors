using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace DumpImmersiveColors
{
    class Program
    {
        [DllImport("uxtheme.dll", EntryPoint = "#95")]
        static extern uint GetImmersiveColorFromColorSetEx(uint nColorSetIndex, uint colorType, bool fNoHighContrast, uint /* IMMERSIVE_HC_CACHE_MODE */ mode);

        [DllImport("uxtheme.dll", EntryPoint = "#98")]
        static extern uint GetImmersiveUserColorSetPreference(bool fForceReloadFromRegkey, bool __formal);

        [DllImport("uxtheme.dll", EntryPoint = "#100")]
        static extern IntPtr GetImmersiveColorNamedTypeByIndex(uint index);


        [DllImport("uxtheme.dll", EntryPoint = "#96", CharSet = CharSet.Unicode)]
        internal static extern uint GetImmersiveColorTypeFromName(string name);

        public static Color ColorFromAbgr(uint abgrValue)
        {
            var colorBytes = new byte[4];
            colorBytes[0] = (byte)((0xFF000000 & abgrValue) >> 24); // A
            colorBytes[1] = (byte)((0x00FF0000 & abgrValue) >> 16); // B
            colorBytes[2] = (byte)((0x0000FF00 & abgrValue) >> 8);  // G
            colorBytes[3] = (byte)(0x000000FF & abgrValue);         // R

            return Color.FromArgb(colorBytes[0], colorBytes[3], colorBytes[2], colorBytes[1]);
        }

        static IDictionary<string, Color> GetImmersiveColors()
        {
            var colors = new Dictionary<string, Color>();
            var colorSet = GetImmersiveUserColorSetPreference(false, false);

            for (uint i = 0; ; i++)
            {
                var ptr = GetImmersiveColorNamedTypeByIndex(i);
                if (ptr == IntPtr.Zero)
                    break;

                var name = Marshal.PtrToStringUni(Marshal.ReadIntPtr(ptr));
                var colorType = GetImmersiveColorTypeFromName(name.Insert(0, "Immersive"));
                colors.Add(name, ColorFromAbgr(GetImmersiveColorFromColorSetEx(colorSet, colorType, false, 0)));
            }

            return colors;
        }

        static void Main(string[] args)
        {
            var colors = GetImmersiveColors();

            if (args.Length > 0 && !String.IsNullOrWhiteSpace(args[0]))
            {
                DumpToImage(colors, args[0]);
            }
            else
            {
                DumpToConsole(colors);
            }
        }

        private static void DumpToConsole(IDictionary<string, Color> colors)
        {
            foreach (var color in colors)
            {
                Console.WriteLine($"{color.Value.ToArgb():X8}\t{color.Key}");
            }

            Console.WriteLine();
        }

        private static void DumpToImage(IDictionary<string, Color> colors, string imagePath)
        {
            var tileSize = new Size(32, 32);
            var tileXPadding = 8;
            var tileYPadding = 16;

            using (var bmp = new Bitmap(500, colors.Count * (tileSize.Height + tileYPadding), PixelFormat.Format32bppArgb))
            {
                using (var gfx = Graphics.FromImage(bmp))
                {
                    gfx.Clear(Color.White);
                    gfx.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                    var i = 0;
                    foreach (var color in colors)
                    {
                        var tilePos = new Point(tileXPadding, (tileSize.Height + tileYPadding) * i++);
                        gfx.FillRectangle(new SolidBrush(color.Value), new Rectangle(tilePos, tileSize));

                        var textPos = new Point(tilePos.X + tileSize.Width + tileXPadding, tilePos.Y + tileSize.Height / 4);
                        gfx.DrawString(color.Key, new Font("Segoe UI", 10.0f, FontStyle.Regular), Brushes.Black, textPos);
                    }

                    bmp.Save(imagePath, ImageFormat.Png);
                    Console.WriteLine("OK.\n");
                }
            }
        }
    }
}