namespace Application;

public sealed class GetPriorWeightQuestionQueryValidator : AbstractValidator<GetPriorWeightQuestionQuery>
{
    public GetPriorWeightQuestionQueryValidator()
    {
        RuleFor(x => x.WeightKg).InclusiveBetween(20m, 400m).WithMessage(ValidationMessages.NumberRange).When(x => x.WeightKg.HasValue);
        RuleFor(x => x.Unit).Must(u => u is "kg" or "lb").WithMessage(ValidationMessages.InvalidFormat).When(x => x.Unit is not null);
    }
}
