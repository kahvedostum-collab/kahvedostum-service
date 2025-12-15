namespace KahveDostum_Service.Domain.Entities;

public enum ReceiptStatus
{
    INIT = 0,
    UPLOADED = 1,
    PROCESSING = 2,
    DONE = 3,
    FAILED = 4
}