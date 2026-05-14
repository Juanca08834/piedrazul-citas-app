namespace Piedrazul.Application;

public static class PiiMasking
{
    public static string? MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 4) return null;
        return $"*** ***-{phone[^4..]}";
    }

    public static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var at = email.IndexOf('@');
        return at > 0 ? $"{email[0]}****{email[at..]}" : null;
    }
}
