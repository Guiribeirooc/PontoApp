namespace PontoApp.Web.Utils
{
    public static class CpfUtils
    {
        public static string OnlyDigits(string? s)
            => string.IsNullOrWhiteSpace(s) ? string.Empty : new string(s.Where(char.IsDigit).ToArray());

        public static string Format(string? s)
        {
            var d = OnlyDigits(s);
            if (d.Length != 11) return s ?? string.Empty;
            return $"{d[..3]}.{d[3..6]}.{d[6..9]}-{d[9..]}";
        }

        public static bool IsValid(string? s)
        {
            var cpf = OnlyDigits(s);
            if (cpf.Length != 11) return false;
            if (cpf.Distinct().Count() == 1) return false;

            int Soma(int len)
            {
                var soma = 0;
                for (int i = 0; i < len; i++)
                    soma += (cpf[i] - '0') * (len + 1 - i);
                return soma;
            }

            var dv1 = (Soma(9) * 10) % 11; if (dv1 == 10) dv1 = 0;
            var dv2 = (Soma(10) * 10) % 11; if (dv2 == 10) dv2 = 0;

            return dv1 == (cpf[9] - '0') && dv2 == (cpf[10] - '0');
        }
    }

    public static class CpfExtensions
    {
        public static string ToCpfMasked(this string? s) => CpfUtils.Format(s);
    }
}
