namespace GuessWhoServices.Communication.Email.Builders
{
    public interface IEmailMessageBuilder<in TContext>
    {
        EmailMessage Build(TContext context);
    }
}
