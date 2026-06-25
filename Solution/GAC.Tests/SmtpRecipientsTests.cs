using GAC.Infrastructure.Services;
using Xunit;

namespace GAC.Tests;

public class SmtpRecipientsTests
{
    [Fact]
    public void Single_Address_Yields_One_Recipient()
        => Assert.Equal(new[] { "a@x.com" }, SmtpEmailSender.ParseRecipients("a@x.com", "from@x.com"));

    [Fact]
    public void Comma_Separated_Yields_Multiple_Recipients()
        => Assert.Equal(new[] { "a@x.com", "b@y.com" },
            SmtpEmailSender.ParseRecipients("a@x.com,b@y.com", "from@x.com"));

    [Fact]
    public void Semicolons_And_Whitespace_Are_Handled_And_Empties_Dropped()
        => Assert.Equal(new[] { "a@x.com", "b@y.com" },
            SmtpEmailSender.ParseRecipients("  a@x.com ; , b@y.com ;; ", "from@x.com"));

    [Fact]
    public void Blank_AdminNotify_Falls_Back_To_FromEmail()
        => Assert.Equal(new[] { "from@x.com" }, SmtpEmailSender.ParseRecipients("   ", "from@x.com"));

    [Fact]
    public void Blank_Everywhere_Yields_No_Recipients()
        => Assert.Empty(SmtpEmailSender.ParseRecipients("", ""));
}
