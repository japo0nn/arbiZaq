using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqRateGetter.Data.Abstract
{
    internal interface IEntity
    {
        Guid Id { get; set; }
        DateTime DateCreated { get; set; }
    }
}
