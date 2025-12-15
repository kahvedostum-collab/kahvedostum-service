namespace KahveDostum_Service.Infrastructure.Options;

public class RabbitOptions
{
    public string Host { get; set; } = default!;
    public int Port { get; set; } = 5672;
    public string User { get; set; } = default!;
    public string Pass { get; set; } = default!;
    public string VHost { get; set; } = "/";

    public string Exchange { get; set; } = "ocr.x";
    public string JobsRoutingKey { get; set; } = "jobs";

    public string ResultsQueue { get; set; } = "ocr.results";
    public string ResultsRoutingKey { get; set; } = "results";
}