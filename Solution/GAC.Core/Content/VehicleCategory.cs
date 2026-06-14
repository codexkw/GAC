namespace GAC.Core.Content;

/// <summary>Body/drivetrain categories. Flags because a model can be e.g. SUV + EV (AION V).</summary>
[Flags]
public enum VehicleCategory
{
    None = 0,
    Sedan = 1,
    Suv = 2,
    Ev = 4
}
