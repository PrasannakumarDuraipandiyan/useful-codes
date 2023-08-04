using Oracle.ManagedDataAccess.Client;

public class OracleLdapConnection
{
    public OracleConnection GetLdapConnection()
    {
        // Replace these with your actual LDAP server and default admin context
        string ldapServers = "ldap://your-ldap-server1:port,ldap://your-ldap-server2:port";
        string defaultAdminContext = "your-default-admin-context";
        
        // Set the directory_server_type based on your LDAP server type (typically OID for Oracle Internet Directory or AD for Active Directory)
        string directoryServerType = "OID";

        // Construct the LDAP connection string with External Naming parameters
        string ldapConnectionString = $"User Id=your-username;Password=your-password;" +
                                      $"Data Source=(DESCRIPTION= " +
                                      $"(ADDRESS_LIST= " +
                                      $"(LOAD_BALANCE=on) " +
                                      $"(FAILOVER=on) " +
                                      $"(ADDRESS=(PROTOCOL=TCP)(HOST={ldapServers})) " +
                                      $")) " +
                                      $"(CONNECT_DATA= " +
                                      $"(SERVER=DEDICATED) " +
                                      $"(SERVICE_NAME=your-service-name) " +
                                      $"(DEFAULT_ADMIN_CONTEXT={defaultAdminContext}) " +
                                      $"(DIRECTORY_SERVER_TYPE={directoryServerType}) " +
                                      $")";

        OracleConnection connection = new OracleConnection(ldapConnectionString);

        return connection;
    }
}
