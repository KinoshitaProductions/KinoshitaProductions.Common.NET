namespace KinoshitaProductions.Common.Enums;

[Flags]
public enum JwtTokenKind
{
    NotSpecified = 0,
    Elevated = 1,
    App = 2,
    Session = 4,
}
