using System.Windows.Forms;
using System.Windows.Input;

public static class KeyConverter
{
    public static Keys ToWinFormsKey(Key wpfKey)
    {
        int virtualKey = KeyInterop.VirtualKeyFromKey(wpfKey);
        return (Keys)virtualKey;
    }

    public static Key ToWpfKey(Keys winFormsKey)
    {
        return KeyInterop.KeyFromVirtualKey((int)winFormsKey);
    }
}