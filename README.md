RS
==

Collectiion of useful development utilities

(Updated: 2014-08-18)

RS is a work in progress. I created this class library with the intention of compiling methods and classes I frequently use into a single location. Highlights include:

##SQL

This namespace contains implementations of the Sytem.Data.SqlClient namespace that facilitate retrieval and modification of data from SQL databases. This namespace accomidates selecting to custom data entities for easy integration into existing solutions. Sample code is as follows:

Instantiating a Database object (connection strings vary, of course):
```
Database db = new Database("Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;");
```

Executing a NonQuery (supports List<SqlParameter> and Timeout arguments):

```
string query = "DELETE * FROM MyDatabase.dbo.Users";

int rowsAffected = db.NonQuery(query);
```

Selecting to a DataTable / Dataset (supports List<SqlParameter> and Timeout arguments):

```
string query = "SELECT * FROM MyDatabase.dbo.Users";

DataTable DT = db.QueryDT(query);

DataSet DS = db.QueryDS(query);
```

Selecting to a custom data entity:

```
[SQLTable("[MyDatabase].[dbo].[Users]")]
public class User
{
  [SqlKeyField(true)]
  public int UserID { get; set; }
  
  public string UserName { get; set; }
  public string PasswordHash { get; set; }
}

public static class UserSelector
{
  public static List<User> GetUsers(string whereString)
  {
    Database db = new Database("Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;");
    
    List<User> myResult = db.GetObjectCollection<User>(WhereString: whereString);
    
    return myResult;
  }
}
```
In the above example, the "User" class is marked with a SQLTable attribute in order to identify its associated table. The property names in this class mirror the property names in the target table. The SQLKeyField attribute marks the UserID property as representing an auto-numbered SQL key field (i.e. "IsIdentity" with a value that increases incrementally); this attribute has no relevance to select statements, however, future updates to this namespace may inlude inserting/merging custom objects, so I've included it here preemptively. When GetObjectCollection is called, a select statement is built using reflection (reflection targets the attributes/properties of the type passed, in this case "User"). The select statement is then used to query a DataTable; subsequently, a List<T> is built during which each DataRow is converted to T, again reflecting off type T.

Further examples will follow...

##Utilities.FileWriter

I developed this generic file writer in order to assist in writing large amounts of data from query results to file. The WriteFile method accepts either a DataTable or a List<T> as an argument, along with parameters related to file format, and handles the header record and variable typing through reflection.

##Utilities.Windows

This namespace houses methods for interacting with program windows - useful, if not overly exciting. Methods to identify and close windows can be particularly useful when one needs to shell out an application to complete a certain process and/or programatically interact with an application's UI (via SendKeys, for example). This stuff coems in handy when pursuing hacky solutions to automating expensive programs that won't expose their APIs without massive fees ;).


