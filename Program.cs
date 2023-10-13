using System.Runtime.InteropServices;

/**
 * D'après https://www.codeproject.com/Articles/90218/Changing-Keyboard-Layout-2 il faut utiliser InputLanguage.CurrentInputLanguage plutôt que de charger user32.dll
 */

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

