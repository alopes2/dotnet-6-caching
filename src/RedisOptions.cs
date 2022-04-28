namespace Cache;

public class RedisOptions
{
    public const string Name = "Redis";
    
    public string ConnectionString { get; set; }

    public string InstanceName { get; set; }
}