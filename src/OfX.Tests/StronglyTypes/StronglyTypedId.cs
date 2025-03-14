﻿namespace OfX.Tests.StronglyTypes;

public record StronglyTypedId<TValue>(TValue Value) where TValue : notnull
{
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
}