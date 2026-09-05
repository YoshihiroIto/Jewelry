using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Jewelry;

public static class TaskExtensions
{
    extension(Task task)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Forget()
        {
        }
    }

    extension<T>(Task<T> task)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Forget()
        {
        }
        
        public async Task<T> WithTimeout(TimeSpan timeout)
        {
            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                return await task;

            throw new TimeoutException("The operation timed out.");
        }
    }

    extension(ValueTask task)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Forget()
        {
        }
    }

    extension<T>(ValueTask<T> task)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Forget()
        {
        }
    }
}