using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Runtime.Serialization;
using DanilovSoft.MicroORM;

namespace MicroORMTests
{
    class UserDbo
    {
        [DataMember(Name = "name")]
        public string Name { get; private set; }

        [SqlProperty("age")]
        public int Age { get; private set; }

        [Column("location")]
        [SqlConverter(typeof(LocationConverter))]
        public Point Location { get; private set; }

        public UserDbo(string name)
        {
            Name = name;
        }
    }
}
