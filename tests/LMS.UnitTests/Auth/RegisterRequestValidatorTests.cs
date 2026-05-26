using FluentAssertions;
using LMS.Application.Auth.Dtos;
using LMS.Application.Auth.Validators;

namespace LMS.UnitTests.Auth;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public async Task Valid_Request_Passes()
    {
        var result = await _validator.ValidateAsync(
            new RegisterRequest("user@example.com", "Password1!", "John", "Doe"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    public async Task Invalid_Email_Fails(string email)
    {
        var result = await _validator.ValidateAsync(
            new RegisterRequest(email, "Password1!", "John", "Doe"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("1234567")]
    public async Task Short_Or_Empty_Password_Fails(string password)
    {
        var result = await _validator.ValidateAsync(
            new RegisterRequest("user@example.com", password, "John", "Doe"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Empty_FirstName_Fails()
    {
        var result = await _validator.ValidateAsync(
            new RegisterRequest("user@example.com", "Password1!", "", "Doe"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public async Task Empty_LastName_Fails()
    {
        var result = await _validator.ValidateAsync(
            new RegisterRequest("user@example.com", "Password1!", "John", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName");
    }
}
