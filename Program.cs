using System;
using System.Drawing;
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

        public static Color FromAbgr(uint abgrValue)
        {
            var colorBytes = new byte[4];
            colorBytes[0] = (byte)((0xFF000000 & abgrValue) >> 24); // A
            colorBytes[1] = (byte)((0x00FF0000 & abgrValue) >> 16); // B
            colorBytes[2] = (byte)((0x0000FF00 & abgrValue) >> 8);  // G
            colorBytes[3] = (byte)(0x000000FF & abgrValue);         // R

            return Color.FromArgb(colorBytes[0], colorBytes[3], colorBytes[2], colorBytes[1]);
        }

        static void Main(string[] args)
        {
            var colorSet = GetImmersiveUserColorSetPreference(false, false);
            for (uint i = 0; ; i++)
            {
                var ptr = GetImmersiveColorNamedTypeByIndex(i);
                if (ptr == IntPtr.Zero)
                    break;

                var name = Marshal.PtrToStringUni(Marshal.ReadIntPtr(ptr));
                var color = FromAbgr(GetImmersiveColorFromColorSetEx(colorSet, i, true, 0)).ToArgb();
                Console.WriteLine($"{color:X8}\t{name}");
            }

            Console.WriteLine();
        }
    }
}
