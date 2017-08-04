项目介绍：
=================
对于符合`ADO.NET`数据操作标准的关系数据库操作适配器提供更加方便的调用方法，大大简化项目中使用ADO的代码数量，节省大量时间<br>
该项目已在生产环境稳定运行一年以上，请放心使用，`Framework.Command.dll`是编译好的文件

基本使用方式：
-----------------

在`web.config`中添加`connectionStrings`配置：
```xml
<connectionStrings>
    <add name="main"
	connectionString="Server=server;Port=3306;Database=database;Uid=user;Pwd=pwd;Allow User Variables=True"
	providerName="MySql.Data.MySqlClient.MySqlConnection, MySql.Data, Version=6.9.8.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d"/>
    <add name="mainByMssql"
	connectionString="Server=server;Port=3306;Database=database;Uid=user;Pwd=pwd;Allow User Variables=True"
	providerName="System.Data.SqlClient"/>
</connectionStrings>
```

在`dao`层编写代码：
```c#
String sql = "SELECT * FROM TableOne WHERE Id=@Id";
String dbName = "main";
Object params = new {
    Id = 999
};

TableOneModel model = Commands.GetCommand(sql, dbName).Read<TableOneModel>(params);
```

或者：
```c#
String sql = "INSERT INTO TableOne(Id, Name) VALUES(@Id, @Name)";
String dbName = "main";
Object params = new {
	Id = 999,
	Name = "名称"
};

bool success = Commands.GetCommand(sql, dbName).Exec(params);
```

`params`支持object对象，也支持任何继承`IDictionary`接口的对象

更多使用方式：
-----------------

`Read<Type>`方法有多种用法，下面列举部分使用方法：
```c#
// 读取一条记录
int count = Commands.GetCommand("SELECT COUNT(*) FROM TableOne", dbName).Read<int>();
// 读取一行实体
TableOneModel model = Commands.GetCommand("SELECT * FROM TableOne WHERE Id=@Id", dbName).Read<TableOneModel>();
// 读取实体列表
List<TableOneModel> models = Commands.GetCommand("SELECT * FROM TableOne", dbName).Read<List<TableOneModel>>();
// 读取多条结果集
Tuple<int, List<TableOneModel>> tupleResult =
	Commands.GetCommand("SELECT COUNT(*) FROM TableOne; SELECT * FROM TableOne", dbName)
	.Read<Tuple<int, List<TableOneModel>>>();
// 读取10条结果集
Tuple<int, int, int, int, int, int, int, Tuple<int, int, int>> tenResult = GetTenResult();

// 执行SQL，并返回受影响的行数
int result = Commands.GetCommand(sql, dbName).Exec(params);
```

事务使用方式：
-----------------

```c#
IDbTransaction trans = Commands.BeginTransaction(dbName);

try {
	// 这里dbNameTemp无效，会强制使用dbName配置
	Commands.GetCommand(sql, dbNameTemp).Exec(params, trans);
	Commands.GetCommand(sql, dbNameTemp).Exec(params, trans);

	trans.Commit();
	// trans.Rollback();
} catch(Exception e) {
	// 这里无需执行trans.Rollback();
	// 当产生SqlException错误时会自动执行trans.Rollback();
	throws e;
}
```
