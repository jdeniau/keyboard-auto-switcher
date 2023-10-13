using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;
/**
 * taken from https://stackoverflow.com/a/38524423/2111353
 * Seems to be a great answer but doesn't work for me.
 */
internal sealed class KeyboardLayout
{
    [DllImport("user32.dll",
    CallingConvention = CallingConvention.StdCall,
    CharSet = CharSet.Unicode,
    EntryPoint = "LoadKeyboardLayout",
    SetLastError = true,
    ThrowOnUnmappableChar = false)]
    static extern uint LoadKeyboardLayout(
    StringBuilder pwszKLID,
    uint flags);

    [DllImport("user32.dll",
        CallingConvention = CallingConvention.StdCall,
        CharSet = CharSet.Unicode,
        EntryPoint = "GetKeyboardLayout",
        SetLastError = true,
        ThrowOnUnmappableChar = false)]
    static extern uint GetKeyboardLayout(
        uint idThread);

    [DllImport("user32.dll",
        CallingConvention = CallingConvention.StdCall,
        CharSet = CharSet.Unicode,
        EntryPoint = "ActivateKeyboardLayout",
        SetLastError = true,
        ThrowOnUnmappableChar = false)]
    static extern uint ActivateKeyboardLayout(
        uint hkl,
        uint Flags);

    [DllImport("user32.dll",
        CallingConvention = CallingConvention.StdCall,
        CharSet = CharSet.Unicode,
        EntryPoint = "GetKeyboardLayoutName",
        SetLastError = true,
        ThrowOnUnmappableChar = false)]
    static extern bool GetKeyboardLayoutName(
        StringBuilder pwszKLID);

    static class KeyboardLayoutFlags
    {
        public const uint KLF_ACTIVATE = 0x00000001;
        public const uint KLF_SETFORPROCESS = 0x00000100;
    }

    private readonly uint hkl;

    public uint Handle
    {
        get
        {
            return this.hkl;
        }
    }


    public KeyboardLayout(CultureInfo cultureInfo)
    {
        Console.WriteLine("cultureInfo: " + cultureInfo.Name);

        string layoutName = cultureInfo.LCID.ToString("x8");

        Console.WriteLine("layoutName: " + layoutName);

        var pwszKlid = new StringBuilder(layoutName);

        Console.WriteLine("GetKeyboardLayoutName " + GetKeyboardLayoutName(pwszKlid));

        this.hkl = LoadKeyboardLayout(pwszKlid, KeyboardLayoutFlags.KLF_ACTIVATE);
    }

    private KeyboardLayout(uint hkl)
    {
        this.hkl = hkl;
    }


    public static KeyboardLayout GetCurrent()
    {
        uint hkl = GetKeyboardLayout((uint)Thread.CurrentThread.ManagedThreadId);


        Console.WriteLine("hkl: " + hkl);

        return new KeyboardLayout(hkl);
    }

    public static KeyboardLayout Load(CultureInfo culture)
    {
        return new KeyboardLayout(culture);
    }

    public void Activate()
    {
        Console.WriteLine("ActivateKeyboardLayout: " + this.hkl);

        ActivateKeyboardLayout(this.hkl, KeyboardLayoutFlags.KLF_SETFORPROCESS);
    }
}