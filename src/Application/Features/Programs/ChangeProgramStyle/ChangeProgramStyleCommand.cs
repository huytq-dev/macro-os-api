namespace Application;

public sealed record ChangeProgramStyleCommand(ProgramType ProgramType) : IRequest<Unit>;
