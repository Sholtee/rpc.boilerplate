using System;
using System.Data;
using System.Linq;

using NUnit.Framework;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

using Tests.Base;

using Solti.Utils.DI.Interfaces;

namespace Services.Tests
{
    [TestFixture]
    public class SQLiteDbConnectionProviderTests: Debuggable
    {
        public class MyTable
        {
            [PrimaryKey]
            public Guid Id { get; set; }

            public string Data { get; set; }
        }

        [Test]
        public void Connection_ShouldAccessTheSameDatabase()
        {
            SQLiteDbConnectionProvider provider = new();

            Guid id = Guid.NewGuid();

            using (IDbConnection conn = provider.GetService<IDbConnection>())
            {
                conn.CreateTable<MyTable>();
                conn.Insert(new MyTable { Id = id, Data = "cica" });
            }

            using (IDbConnection conn = provider.GetService<IDbConnection>())
            {
                Assert.That(conn.TableExists<MyTable>());

                MyTable table = conn.SelectByIds<MyTable>(new[] { id }).SingleOrDefault();
                Assert.That(table, Is.Not.Null);
                Assert.That(table.Data, Is.EqualTo("cica"));
            }
        }

        [Test]
        public void Connection_ShouldAccessTheSameDatabase2()
        {
            SQLiteDbConnectionProvider provider = new();

            Guid id = Guid.NewGuid();

            using (IDbConnection conn = provider.GetService<IDbConnection>())
            {
                conn.CreateTable<MyTable>();
                conn.Insert(new MyTable { Id = id, Data = "cica" });

                using (IDbConnection conn2 = provider.GetService<IDbConnection>())
                {
                    Assert.That(conn2.TableExists<MyTable>());

                    MyTable table = conn2.SelectByIds<MyTable>(new[] { id }).SingleOrDefault();
                    Assert.That(table, Is.Not.Null);
                    Assert.That(table.Data, Is.EqualTo("cica"));
                }
            }
        }
    }
}
