using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ItemSense
{
    public enum ResponseCode
    {
        Success = 200,
        NoContent = 204,
        Failure = 400,
        Unauthorized = 401,
        ResourceNotAvailable = 404,
    }
}
