using System.Text.RegularExpressions;

namespace Piedrazul.Application;

public sealed record TimeSlot(TimeOnly StartTime, TimeOnly EndTime);

public static class BookingSlotCalculator
{
    public static IReadOnlyList<TimeSlot> BuildSlots(TimeOnly start, TimeOnly end, int intervalMinutes)
    {
        var slots = new List<TimeSlot>();

        if (intervalMinutes <= 0 || start >= end)
        {
            return slots;
        }

        var current = start;
        while (current < end)
        {
            var next = current.AddMinutes(intervalMinutes);
            if (next > end)
            {
                break;
            }

            slots.Add(new TimeSlot(current, next));
            current = next;
        }

        return slots;
    }
}

public static class PatientInputValidator
{
    private static readonly Regex DigitsOnly = new("^[0-9]+$", RegexOptions.Compiled);
    private static readonly Regex PersonName = new("^[A-Za-zÁÉÍÓÚáéíóúÑñÜü' -]+$", RegexOptions.Compiled);

    public static string Normalize(string? value)
    {
        return Regex.Replace(value?.Trim() ?? string.Empty, "\\s+", " ");
    }

    public static IReadOnlyList<string> ValidateBasicPatientData(
        string documentNumber,
        string firstName,
        string lastName,
        string phone,
        string? email)
    {
        var errors = new List<string>();
        var normalizedDocument = Normalize(documentNumber);
        var normalizedFirstName = Normalize(firstName);
        var normalizedLastName = Normalize(lastName);
        var normalizedPhone = Normalize(phone);
        var normalizedEmail = Normalize(email);

        if (normalizedDocument.Length is < 5 or > 20 || !DigitsOnly.IsMatch(normalizedDocument))
        {
            errors.Add("El documento debe contener solo números y tener entre 5 y 20 dígitos.");
        }

        if (normalizedFirstName.Length is < 2 or > 80 || !PersonName.IsMatch(normalizedFirstName))
        {
            errors.Add("Los nombres solo pueden contener letras, espacios, apóstrofes o guiones y tener entre 2 y 80 caracteres.");
        }

        if (normalizedLastName.Length is < 2 or > 80 || !PersonName.IsMatch(normalizedLastName))
        {
            errors.Add("Los apellidos solo pueden contener letras, espacios, apóstrofes o guiones y tener entre 2 y 80 caracteres.");
        }

        if (normalizedPhone.Length is < 7 or > 15 || !DigitsOnly.IsMatch(normalizedPhone))
        {
            errors.Add("El celular debe contener solo números y tener entre 7 y 15 dígitos.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedEmail) && (normalizedEmail.Length > 150 || !normalizedEmail.Contains('@')))
        {
            errors.Add("El correo electrónico no tiene un formato válido o supera los 150 caracteres.");
        }

        return errors;
    }
}

public static class ScheduleValidator
{
    public static IReadOnlyList<string> ValidateInterval(int intervalMinutes)
    {
        var errors = new List<string>();
        if (intervalMinutes is < 10 or > 120)
        {
            errors.Add("El intervalo entre citas debe estar entre 10 y 120 minutos.");
        }

        return errors;
    }
}
