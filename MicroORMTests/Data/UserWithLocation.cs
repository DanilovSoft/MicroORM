using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Runtime.Serialization;
using DanilovSoft.MicroORM;

namespace MicroORMTests
{
    class UserWithLocation
    {
        [Column("location")]
        [TypeConverter(typeof(LocationConverter))]
        public Point Location { get; private set; }
    }
}
