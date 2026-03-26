namespace DataMaskingSystem
{
    internal static class CustomStringHelper
    {
        public static int GetLength(string input)
        {
            if (input == null) return 0;
            int len = 0;
            foreach (char c in input) len++;
            return len;
        }

        public static char[] ToCharArray(string input)
        {
            if (input == null) return new char[0];
            int len = GetLength(input);
            char[] result = new char[len];
            int i = 0;
            foreach (char c in input)
            {
                result[i] = c;
                i++;
            }
            return result;
        }
    }

    internal static class CustomHash
    {
        public static char[] ComputeHash(char[] input)
        {
            if (input == null) return new char[0];
            int len = 0;
            foreach (char c in input) len++;

            char[] hash = new char[16];
            for (int i = 0; i < 16; i++) hash[i] = '0';
            for (int i = 0; i < len; i++)
            {
                int val = input[i];
                val = ((val << 3) | (val >> 5)) ^ (i * 17);
                int index = i % 16;
                hash[index] = (char)((hash[index] + val) % 26 + 'A');
            }
            return hash;
        }
    }
}
