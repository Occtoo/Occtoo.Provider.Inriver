namespace Occtoo.Generic.Inriver.Extensions
{
    public static class StringExtensions
    {
        public static bool ForceParseBool(this string str)
        {
            return bool.TryParse(str, out var b) && b;
        }
    }
}