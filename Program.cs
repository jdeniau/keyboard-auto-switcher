using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Text;
using System.IO;
using System.Drawing.Text;
using System.Collections.Generic;
using System.Management; // need to add System.Management to your project references.
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Globalization;



/**
 * D'après https://www.codeproject.com/Articles/90218/Changing-Keyboard-Layout-2 il faut utiliser InputLanguage.CurrentInputLanguage plutôt que de charger user32.dll
 */


class USBDeviceInfo
{

    public static string KeyboardInstanceName = "USB\\VID_1E54&PID_2030\\";

    public USBDeviceInfo(string deviceID, string pnpDeviceID, string description)
    {
        this.DeviceID = deviceID;
        this.PnpDeviceID = pnpDeviceID;
        this.Description = description;
    }
    public string DeviceID { get; private set; }
    public string PnpDeviceID { get; private set; }
    public string Description { get; private set; }


    public static List<USBDeviceInfo> GetUSBDevices()
    {
        List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

        using var searcher = new ManagementObjectSearcher(
            @"Select * From Win32_USBHub");
        using ManagementObjectCollection collection = searcher.Get();

        foreach (var device in collection)
        {
            devices.Add(new USBDeviceInfo(
                (string)device.GetPropertyValue("DeviceID"),
                (string)device.GetPropertyValue("PNPDeviceID"),
                (string)device.GetPropertyValue("Description")
                ));
        }
        return devices;
    }
}

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

class MyApplicationContext : ApplicationContext
{

    private MyApplicationContext()
    {
        // First start : let's init current input language
        SetNewCurrentLanguage(GetCurrentInputLanguage());

        LoopAndWaitForKeyboardChange();
    }

    private void LoopAndWaitForKeyboardChange()
    {
        do
        {
            if (IsKeyboardConnected())
            {
                Console.WriteLine("Keyboard is connected.");

                // new KeyboardLayout(CultureInfo.GetCultureInfo("en-US")).Activate();

                if (GetCurrentInputLanguage() != "en-US")
                {
                    Console.WriteLine("not en-US, switching…");

                    SetNewCurrentLanguage("en-US");
                    NextKeyboard();
                }
            }
            else
            {
                Console.WriteLine("Keyboard is not connected.");

                // new KeyboardLayout(CultureInfo.GetCultureInfo("fr-FR")).Activate();

                if (GetCurrentInputLanguage() != "fr-FR")
                {
                    Console.WriteLine("not fr-FR, switching…");

                    SetNewCurrentLanguage("fr-FR");
                    NextKeyboard();
                }
            }

            // sleep 5 seconds
            System.Threading.Thread.Sleep(5000);
        } while (true);
    }


    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);



    private const int KEYEVENTF_EXTENDEDKEY = 1;
    private const int KEYEVENTF_KEYUP = 2;

    public void NextKeyboard()
    {

        KeyDown(Keys.LWin);
        KeyDown(Keys.Space);
        KeyUp(Keys.Space);
        KeyUp(Keys.LWin);
    }

    public void KeyDown(Keys vKey)
    {
        keybd_event((byte)vKey, 0, KEYEVENTF_EXTENDEDKEY, 0);
    }

    public void KeyUp(Keys vKey)
    {
        keybd_event((byte)vKey, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
    }

    private string GetCurrentInputLanguage()
    {
        Console.WriteLine(
            "Current language is: [input] " +
            InputLanguage.CurrentInputLanguage.Culture.Name +
            " [thread] " +
            Thread.CurrentThread.CurrentCulture.Name
        );

        return InputLanguage.CurrentInputLanguage.Culture.Name;
    }

    /** CurrentInputLanguage setter is not working ? */
    void SetNewCurrentLanguage(string culture)
    {

        if (InputLanguage.CurrentInputLanguage.Culture.Name == culture)
        {
            Console.WriteLine("The current input language is already: " + culture + ", do nothing.");
            return;
        }

        InputLanguage? nextInputLanguage = InputLanguage.InstalledInputLanguages
            .OfType<InputLanguage>()
            .Where(l => l.Culture.Name == culture)
            .FirstOrDefault();


        if (nextInputLanguage == null)
        {
            Console.Error.WriteLine("The culture " + culture + " is not installed on this computer.");

            return;
        }

        // NOT WORKING ?
        // InputLanguage.CurrentInputLanguage = nextInputLanguage;

        // WORKING !
        InputLanguage.CurrentInputLanguage = InputLanguage.InstalledInputLanguages[InputLanguage.InstalledInputLanguages.IndexOf(nextInputLanguage)];
        Thread.CurrentThread.CurrentCulture = nextInputLanguage.Culture;
    }


    private bool IsKeyboardConnected()
    {
        var usbDevices = USBDeviceInfo.GetUSBDevices();

        return usbDevices.Where(d => d.PnpDeviceID.StartsWith(USBDeviceInfo.KeyboardInstanceName)).Any();
    }



    [STAThread]
    static void Main(string[] args)
    {

        // Create the MyApplicationContext, that derives from ApplicationContext,
        // that manages when the application should exit.

        MyApplicationContext context = new MyApplicationContext();

        // Run the application with the specific context. It will exit when
        // all forms are closed.
        Application.Run(context);
    }
}

