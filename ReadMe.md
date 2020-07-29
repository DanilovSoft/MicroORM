```csharp
class UserModel
{
    [DataMember(Name = "name")]
    public string Name { get; private set; }

    [SqlProperty("age")]
    public int Age { get; private set; }

    [SqlProperty("location")]
    [SqlConverter(typeof(LocationConverter))]
    public Point Location { get; private set; }
}

List<UserModel> list = _orm.Sql("SELECT * FROM users")
    .Timeout(timeoutSec: 30)
    .List<UserModel>();


string result = _orm.Sql("SELECT @0")
    .Parameter("OK")
    .Scalar<string>();


UserModel result = _orm.Sql("SELECT point(@0, @1) AS location")
    .Parameters(1, 2)
    .Single<UserModel>();


var result = _orm.Sql("SELECT @name AS name, @count AS count")
    .ParametersFromObject(new { count = 128, name = "Alfred" })
    .SingleOrDefault<UserModel>();


var result = _orm.Sql("SELECT @name AS name, @age AS age")
    .Parameter("name", "Alfred")
    .Parameter("age", 30)
    .Single(new { name = "", age = 0 });


var result = await _orm.Sql("SELECT @name AS qwer, @age AS a")
    .Parameter("name", "Alfred")
    .Parameter("age", 30)
    .ToAsync()
    .List(new { name = 0, age = "" }, CancellationToken.None);


decimal[] result = _orm.Sql("SELECT unnest(array['1', '2', '3'])")
    .ScalarArray<decimal>();


List<(string name, int age)> rows = _pgsql.Sql("SELECT * FROM table1")
    .AsAnonymous(new { name = default(string), age = default(int) })
    .List(x => (x.name, x.age));
                

await _orm.Sql("SELECT pg_sleep(10)")
    .Timeout(timeoutSec: 5)
    .ToAsync()
    .Execute();


using (var t = _orm.OpenTransaction())
{
    byte result = t.Sql("SELECT @count")
        .Parameter("count", 128)
        .Scalar<byte>();
        
    t.Commit();
}


using (var multiResult = _orm.Sql("SELECT @0 AS row1; SELECT unnest(array['1', '2'])")
    .Parameters(1)
    .MultiResult())
{
    int row1 = multiResult.Scalar<int>();
    string[] row2 = multiResult.ScalarArray<string>();
}
```