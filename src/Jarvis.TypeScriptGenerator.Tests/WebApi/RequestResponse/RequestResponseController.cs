using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Jarvis.TypeScriptGenerator.Tests.WebApi.RequestResponse
{
    public class RequestResponseController : ApiController
    {
        [HttpPost]
        public ResponseModel Post(RequestModel req)
        {
            return new ResponseModel();
        }

        public IEnumerable<ResponseEnumerableItemModel> GetMulti()
        {
            return new[] { new ResponseEnumerableItemModel() };
        }

        public Object GetAny()
        {
            return new { key = "value" };
        }

        public Object GetById(string id)
        {
            return new { id = id };
        }

        //[HttpPost]
        //public Object GetByIdAndDate(string id, DateTime date)
        //{
        //    return new { id = id };
        //}
    }
}
