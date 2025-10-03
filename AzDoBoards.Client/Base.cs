namespace AzDoBoards.Client;

public class Base
{
    protected readonly ConnectionFactory _connectionFactory;

    public Base(ConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
}
