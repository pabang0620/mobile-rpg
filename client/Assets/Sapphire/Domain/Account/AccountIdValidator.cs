namespace Sapphire.Domain.Account
{
    /// <summary>
    /// Minimal validation for the local-account id entered on the login
    /// screen. docs/planning/01_PRODUCT.md's "로컬 시작 흐름"/"로컬 슬롯 1개"
    /// scope has no server auth and no password - this only guards against
    /// an empty id (which would otherwise become an unusable save-file name).
    /// </summary>
    public static class AccountIdValidator
    {
        public const int MinLength = 2;
        public const int MaxLength = 16;

        public static bool IsValid(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return false;
            }

            int length = accountId.Trim().Length;
            return length >= MinLength && length <= MaxLength;
        }
    }
}
