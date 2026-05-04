using Piedrazul.Application;
using Xunit;

namespace Piedrazul.Domain.Tests;

public sealed class OperationResultTests
{
    // ── Success ───────────────────────────────────────────────────────────────

    [Fact]
    public void Success_ShouldHaveSucceededTrue()
    {
        var result = OperationResult<string>.Success("datos");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Success_ShouldContainData()
    {
        var result = OperationResult<string>.Success("datos");

        Assert.Equal("datos", result.Data);
    }

    [Fact]
    public void Success_ShouldHaveEmptyErrors()
    {
        var result = OperationResult<string>.Success("datos");

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Success_ShouldHaveSuccessStatus()
    {
        var result = OperationResult<int>.Success(42);

        Assert.Equal(OperationStatus.Success, result.Status);
    }

    // ── ValidationError ───────────────────────────────────────────────────────

    [Fact]
    public void Validation_ShouldHaveSucceededFalse()
    {
        var result = OperationResult<string>.Validation("Error 1");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Validation_ShouldContainAllErrors()
    {
        var result = OperationResult<string>.Validation("Error 1", "Error 2");

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("Error 1", result.Errors);
        Assert.Contains("Error 2", result.Errors);
    }

    [Fact]
    public void Validation_ShouldHaveValidationErrorStatus()
    {
        var result = OperationResult<string>.Validation("err");

        Assert.Equal(OperationStatus.ValidationError, result.Status);
    }

    [Fact]
    public void Validation_DataShouldBeNull()
    {
        var result = OperationResult<string>.Validation("err");

        Assert.Null(result.Data);
    }

    // ── NotFound ──────────────────────────────────────────────────────────────

    [Fact]
    public void NotFound_ShouldHaveNotFoundStatus()
    {
        var result = OperationResult<string>.NotFound("No encontrado");

        Assert.Equal(OperationStatus.NotFound, result.Status);
    }

    [Fact]
    public void NotFound_ShouldHaveSucceededFalse()
    {
        var result = OperationResult<string>.NotFound("No encontrado");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void NotFound_ShouldContainErrorMessage()
    {
        var result = OperationResult<string>.NotFound("No encontrado");

        Assert.Contains("No encontrado", result.Errors);
    }

    // ── Conflict ──────────────────────────────────────────────────────────────

    [Fact]
    public void Conflict_ShouldHaveConflictStatus()
    {
        var result = OperationResult<string>.Conflict("Conflicto de datos");

        Assert.Equal(OperationStatus.Conflict, result.Status);
    }

    [Fact]
    public void Conflict_ShouldHaveSucceededFalse()
    {
        var result = OperationResult<string>.Conflict("Conflicto de datos");

        Assert.False(result.Succeeded);
    }
}
