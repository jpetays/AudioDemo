using System.Globalization;
using System.Text;

namespace Prg.Util
{
    public static class PlatformUtil
    {
        public static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
        public static readonly Encoding Encoding = new UTF8Encoding(false, false);
    }
}
