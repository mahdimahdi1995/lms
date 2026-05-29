namespace LMS.Domain.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Trainer = "Trainer";
    public const string Learner = "Learner";

    public static readonly IReadOnlyList<string> All = [Admin, Manager, Trainer, Learner];
}
