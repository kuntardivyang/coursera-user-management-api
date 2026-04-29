using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class NotWhitespaceAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is null) return true; // let [Required] decide on null
        if (value is string s) return !string.IsNullOrWhiteSpace(s);
        return true;
    }

    public override string FormatErrorMessage(string name) =>
        $"The {name} field cannot be blank or whitespace.";
}
