using Piedrazul.Application;
using Xunit;

namespace Piedrazul.Domain.Tests;

public sealed class PiiMaskingTests
{
    // ── MaskPhone ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("+57 3001234567", "*** ***-4567")]
    [InlineData("3101234567",     "*** ***-4567")]
    [InlineData("12345678",       "*** ***-5678")]
    [InlineData("1234",           "*** ***-1234")]
    public void MaskPhone_DebeEnmascararLosUltimosCuatroDigitos(string phone, string expected)
    {
        Assert.Equal(expected, PiiMasking.MaskPhone(phone));
    }

    [Fact]
    public void MaskPhone_DebeRetornarNull_CuandoEsNulo()
    {
        Assert.Null(PiiMasking.MaskPhone(null));
    }

    [Fact]
    public void MaskPhone_DebeRetornarNull_CuandoEsEspacioEnBlanco()
    {
        Assert.Null(PiiMasking.MaskPhone("   "));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    public void MaskPhone_DebeRetornarNull_CuandoTieneMenosDeCuatroCaracteres(string phone)
    {
        Assert.Null(PiiMasking.MaskPhone(phone));
    }

    [Fact]
    public void MaskPhone_NoDebeExponerDigitosDelanterosDelNumero()
    {
        var resultado = PiiMasking.MaskPhone("3001234567");
        Assert.NotNull(resultado);
        Assert.DoesNotContain("300", resultado);
        Assert.DoesNotContain("123", resultado);
    }

    // ── MaskEmail ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("juan@piedrazul.local",    "j****@piedrazul.local")]
    [InlineData("admin@example.com",       "a****@example.com")]
    [InlineData("paciente@demo.test",      "p****@demo.test")]
    [InlineData("a@b.co",                  "a****@b.co")]
    public void MaskEmail_DebeEnmascararNombreDeUsuario(string email, string expected)
    {
        Assert.Equal(expected, PiiMasking.MaskEmail(email));
    }

    [Fact]
    public void MaskEmail_DebeRetornarNull_CuandoEsNulo()
    {
        Assert.Null(PiiMasking.MaskEmail(null));
    }

    [Fact]
    public void MaskEmail_DebeRetornarNull_CuandoEsEspacioEnBlanco()
    {
        Assert.Null(PiiMasking.MaskEmail("   "));
    }

    [Theory]
    [InlineData("sinArroba.com")]
    [InlineData("nodomain")]
    public void MaskEmail_DebeRetornarNull_CuandoNoContieneArroba(string email)
    {
        Assert.Null(PiiMasking.MaskEmail(email));
    }

    [Fact]
    public void MaskEmail_DebeRetornarNull_CuandoArrobaEstaAlInicio()
    {
        Assert.Null(PiiMasking.MaskEmail("@dominio.com"));
    }

    [Fact]
    public void MaskEmail_ConservaDominioCorrecto()
    {
        var resultado = PiiMasking.MaskEmail("usuario@piedrazul.local");
        Assert.NotNull(resultado);
        Assert.EndsWith("@piedrazul.local", resultado);
    }

    [Fact]
    public void MaskEmail_NoExponeMasDeUnCaracterDelNombreDeUsuario()
    {
        var resultado = PiiMasking.MaskEmail("juanito@example.com");
        Assert.NotNull(resultado);
        Assert.StartsWith("j", resultado);
        Assert.DoesNotContain("uanito", resultado);
    }
}
