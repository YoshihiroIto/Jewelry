using System.Runtime.CompilerServices;

namespace Filedini.Foundation;

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