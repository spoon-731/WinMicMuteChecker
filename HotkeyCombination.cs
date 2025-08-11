using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WinMicMuteChecker
{
    [Serializable]
    public class HotkeyCombination(params Keys[] keys)
    {
        public List<Keys> CombinationKeys { get; set; } = [.. keys];

        public override string ToString() => string.Join(" + ", CombinationKeys);

        public bool IsMatch(HashSet<Keys> currentlyPressed)
        {
            var normalizedPressed = currentlyPressed.Select(NormalizeKey).ToHashSet();
            var normalizedHotkey = CombinationKeys.Select(NormalizeKey).ToHashSet();

            return normalizedPressed.SetEquals(normalizedHotkey);
        }

        private Keys NormalizeKey(Keys key)
        {
            if (key == Keys.LShiftKey || key == Keys.RShiftKey)
                return Keys.ShiftKey;
            if (key == Keys.LControlKey || key == Keys.RControlKey)
                return Keys.ControlKey;
            if (key == Keys.LWin || key == Keys.RWin)
                return Keys.LWin;
            if (key == Keys.LMenu || key == Keys.RMenu)
                return Keys.Menu;
            return key;
        }
    }
}
