using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateFileFinder
{
    public static class TaskExtension
    {
        public static async Task UntilCancelled(this Task task)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {

            }
        }
    }
}
