namespace Lib;

public record HttpRequestConfiguration(
    string ClientName,
    Uri Uri,
    HttpMethod Method);
