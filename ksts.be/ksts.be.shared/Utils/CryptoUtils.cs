using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ksts.be.shared.Utils
{
    public static class CryptoUtils
    {
        private static readonly char[] UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private static readonly char[] LowerChars = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
        private static readonly char[] DigitChars = "0123456789".ToCharArray();
        private static readonly char[] SpecialChars = "!@#$%^&*()-_=+[]{};:,.<>?/".ToCharArray();
        private static readonly char[] AllChars =
            UpperChars.Concat(LowerChars).Concat(DigitChars).Concat(SpecialChars).ToArray();

        public static string GenerateRandomString(int length)
        {
            if (length < 4)
                throw new ArgumentException("Length must be at least 4 to fit all required character types.");

            var random = new Random();
            var result = new StringBuilder();

            // Ensure each required type is included at least once
            result.Append(UpperChars[random.Next(UpperChars.Length)]);
            result.Append(LowerChars[random.Next(LowerChars.Length)]);
            result.Append(DigitChars[random.Next(DigitChars.Length)]);
            result.Append(SpecialChars[random.Next(SpecialChars.Length)]);

            // Fill the rest randomly
            for (int i = result.Length; i < length; i++)
            {
                result.Append(AllChars[random.Next(AllChars.Length)]);
            }

            // Shuffle to avoid predictable order
            return new string(result.ToString().OrderBy(_ => random.Next()).ToArray());
        }
    }
}

