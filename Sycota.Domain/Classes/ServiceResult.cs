using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sycota.Domain.Classes
{
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public T Data { get; set; }

        public static ServiceResult<T> Ok(T data) => new ServiceResult<T> { Success = true, Data = data };
        public static ServiceResult<T> Fail(string error) => new ServiceResult<T> { Success = false, Error = error };
    }

    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }

        public static ServiceResult Ok() => new ServiceResult { Success = true };
        public static ServiceResult Fail(string error) => new ServiceResult { Success = false, Error = error };
    }
}
