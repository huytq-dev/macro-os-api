namespace Application;

public sealed class GetFoodByBarcodeQueryValidator : AbstractValidator<GetFoodByBarcodeQuery>
{
    public GetFoodByBarcodeQueryValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage(ValidationMessages.Required)
            .Matches(@"^\d{8,14}$").WithMessage(ValidationMessages.InvalidBarcode);
    }
}
