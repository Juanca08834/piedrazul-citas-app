using Piedrazul.Application;

namespace Piedrazul.Domain.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void BuildSlots_ShouldGenerateExpectedSlots()
    {
        var slots = BookingSlotCalculator.BuildSlots(new TimeOnly(8, 0), new TimeOnly(9, 0), 30);

        Assert.Equal(2, slots.Count);
        Assert.Equal(new TimeOnly(8, 0), slots[0].StartTime);
        Assert.Equal(new TimeOnly(8, 30), slots[0].EndTime);
        Assert.Equal(new TimeOnly(8, 30), slots[1].StartTime);
        Assert.Equal(new TimeOnly(9, 0), slots[1].EndTime);
    }

    [Fact]
    public void ValidateBasicPatientData_ShouldRejectInvalidDocumentAndPhone()
    {
        var errors = PatientInputValidator.ValidateBasicPatientData("ABC", "Ana", "Gomez", "30A", "correo");

        Assert.Contains(errors, error => error.Contains("documento", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("celular", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("correo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Normalize_ShouldTrimAndCollapseSpaces()
    {
        var normalized = PatientInputValidator.Normalize("  Ana    Maria  ");

        Assert.Equal("Ana Maria", normalized);
    }
}
