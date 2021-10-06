using System;
using System.Collections.Generic;
using System.Text;

namespace CacheClientInfrascture.Redis.Exceptions
{

    


    public enum eRedisExceptionType
    {
        AcquireKeyIsNull,
        AcquireLockValueIsNull,
        ReleaseKeyIsNull,
        ReleaseLockValueIsNull,
        GetKeyIsNull
    }

    public class ExceptionFactory
    {
        public static Exception Create(eRedisExceptionType type)
        {
            switch (type)
            {
                case eRedisExceptionType.GetKeyIsNull:
                    return new ArgumentNullException("GetKeyIsNull");
                case eRedisExceptionType.AcquireKeyIsNull:
                    return new ArgumentNullException("AcquireKeyIsNull");
                case eRedisExceptionType.AcquireLockValueIsNull:
                    return new ArgumentNullException("AcquireLockValueIsNull");
                case eRedisExceptionType.ReleaseKeyIsNull:
                    return new ArgumentNullException("ReleaseKeyIsNull");
                case eRedisExceptionType.ReleaseLockValueIsNull:
                    return new ArgumentNullException("ReleaseLockValueIsNull");
                default:
                    throw new Exception($"eRedisExceptionType doesNotExists: {type}");
            }
        }
    }
}
