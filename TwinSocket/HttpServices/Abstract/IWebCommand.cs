using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelayProtection.Services.HttpServices.Abstract
{
    public interface IWebCommand
    {
        string CommandName { get; }
        string ToSerializedString();
    }
}
