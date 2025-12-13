namespace KahveDostum_Service.Application.Helpers;

public static class AddressNormalizer
{
    public static string Normalize(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return string.Empty;

        return address
            .ToLowerInvariant()
            .Replace("mah.", "mahalle")
            .Replace("mh.", "mahalle")
            .Replace("cad.", "cadde")
            .Replace("cd.", "cadde")
            .Replace("sk.", "sokak")
            .Replace(".", "")
            .Replace(",", "")
            .Trim();
    }
}