using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

public partial class HotkeyDialog : Window
{
    public Keys Key { get; private set; }
    public ModifierKeys Modifier { get; private set; }

    public HotkeyDialog()
    {
        InitializeComponent();
    }

    private void HotkeyBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        Modifier = Keyboard.Modifiers;
        Key = (Keys)KeyInterop.VirtualKeyFromKey(e.Key);
        HotkeyBox.Text = Modifier + " + " + Key;
        e.Handled = true;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
