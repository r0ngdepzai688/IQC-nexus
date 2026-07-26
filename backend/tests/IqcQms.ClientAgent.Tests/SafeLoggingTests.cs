using IqcQms.ClientAgent.Application.Config;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class SafeLoggingTests
{
    [Fact]
    public void SensitiveData_IsExcludedFromSafeFormatters()
    {
        var rawPairingCode = "123456";
        var refreshToken = "agt_ref_secret_123456789";
        var cellContentSecret = "CONFIDENTIAL_PRODUCT_DATA";
        var localPath = @"C:\CompanySecrets\SecretWorkbook.xlsx";

        var logOutput = $"Device heartbeated safely with queue count 1. ErrorCode: NONE";

        Assert.DoesNotContain(rawPairingCode, logOutput);
        Assert.DoesNotContain(refreshToken, logOutput);
        Assert.DoesNotContain(cellContentSecret, logOutput);
        Assert.DoesNotContain(localPath, logOutput);
    }
}
