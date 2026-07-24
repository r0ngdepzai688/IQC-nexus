using IqcQms.ClientAgent.Contracts;

namespace IqcQms.Infrastructure.Security;

public interface INormalizedWorkbookCanonicalizer
{
    byte[] CanonicalizeWorkbook(NormalizedWorkbook workbook);
    string ComputeSourceFingerprint(NormalizedWorkbook workbook);
}
