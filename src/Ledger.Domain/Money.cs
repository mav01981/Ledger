namespace Ledger.Domain;

public readonly record struct Money(decimal Amount)
{
    public static Money Zero => new(0m);
    public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount);
    public static Money operator -(Money a, Money b) => new(a.Amount - b.Amount);
    public static Money operator -(Money m) => new(-m.Amount);
    public bool IsNegative => Amount < 0;
    public bool IsZero => Amount == 0;
    public override string ToString() => Amount.ToString("0.00");
}
