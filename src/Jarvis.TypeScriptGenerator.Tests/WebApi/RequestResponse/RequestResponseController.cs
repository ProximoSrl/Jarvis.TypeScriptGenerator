﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Jarvis.TypeScriptGenerator.Tests.WebApi.RequestResponse
{
    public class RequestResponseController : ApiController
    {
        public ResponseModel Post(RequestModel req)
        {
            return new ResponseModel();
        }
    }
}
