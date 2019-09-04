using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    public interface ISqlConverter
    {
        object Convert(object value, Type destinationType);
    }
}
