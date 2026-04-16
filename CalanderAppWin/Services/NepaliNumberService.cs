namespace NepaliCalendar.App.Services
{
    public class NepaliNumberService
    {
        private static readonly char[] NepaliDigits =
        {
            '०', '१', '२', '३', '४', '५', '६', '७', '८', '९'
        };

        public string ToNepaliNumber(int number)
        {
            return ToNepaliNumber(number.ToString());
        }

        public string ToNepaliNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var chars = value.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsDigit(chars[i]))
                {
                    chars[i] = NepaliDigits[chars[i] - '0'];
                }
            }

            return new string(chars);
        }
    }
}