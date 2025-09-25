namespace Scm.Application.Validators;

public interface IValidator<in T>
{
    IEnumerable<string> Validate(T instance);
}
