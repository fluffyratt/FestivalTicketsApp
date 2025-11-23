namespace FestivalTicketsApp.Shared;

public abstract class ResultBase
{
    public readonly Error? Error;

    public readonly bool IsSuccess;

    protected ResultBase(bool isSuccess, Error? error)
    {
        if ((isSuccess && error is not null) ||
            (!isSuccess && error is null))
            throw new ArgumentException("Invalid result creation", nameof(error));

        IsSuccess = isSuccess;
        Error = error;
    }
}

public class Result : ResultBase
{
    private Result(bool isSuccess, Error? error) : base(isSuccess, error)
    { }
    
    public static Result Success() => new(true, null);
    
    public static Result Failure(Error errorInstance) => new(false, errorInstance);
}

public class Result<TValue> : ResultBase
{
    public readonly TValue? Value;
    private Result(bool isSuccess, Error? error, TValue? value) : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<TValue> Success(TValue value) => new(true, null, value);
    
    public static Result<TValue> Failure(Error errorInstance, TValue? optionalErrorResult = default) 
        => new(false, errorInstance, optionalErrorResult);
}