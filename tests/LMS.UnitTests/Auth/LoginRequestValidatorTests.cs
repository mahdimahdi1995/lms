using FluentAssertions;
using LMS.Application.Auth.Dtos;
using LMS.Application.Auth.Validators;

namespace LMS.UnitTests.Auth;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public async Task Valid_Request_Passes()
    {
        var result = await _validator.ValidateAsync(
            new LoginRequest("user@example.com", "Password1!"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task Invalid_Email_Fails(string email)
    {
        var result = await _validator.ValidateAsync(
            new LoginRequest(email, "Password1!"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Empty_Password_Fails()
    {
        var result = await _validator.ValidateAsync(
            new LoginRequest("user@example.com", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
