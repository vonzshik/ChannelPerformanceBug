using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TestingChannels
{
    class Program
    {
        static Pool pool;

        static async Task Main(string[] args)
        {
            pool = new Pool();

            await Task.WhenAll(Enumerable.Range(0, 1000).Select(x => NpgsqlIssue(x)));
        }

        static async Task NpgsqlIssue(int i)
        {
            // The steps are:
            // 1. Rent from the pool asynchronously
            // 2. Do some async work (so we move to the thread pool)
            // 3. Attempt to rent synchronously

            Console.WriteLine("Start");
            var obj = await pool.Rent(true);
            Console.WriteLine("First rent successful");

            // Queing some async operation
            await Task.Delay(100); // Removing this fixes
            // We're now on the thread pool

            pool.Return(obj);

            Console.WriteLine("Starting second rent");
            obj = await pool.Rent(false); // Making rent async fixes
            Console.WriteLine("Second rent successful");

            pool.Return(obj);
        }

        /*
        // Deadlocks
        static async Task Test(int i)
        {
            // Now we're running on the thread from the threadpool
            await Task.Delay(100);

            var async = i % 2 == 0;
            var start = DateTime.UtcNow;
            var obj = await pool.Rent(async);
            obj.RentDate = DateTime.UtcNow;
            if (obj.ReturnDate.HasValue)
                Console.WriteLine($"There was a {(obj.RentDate - obj.ReturnDate.Value).TotalMilliseconds}ms delay between obj return and rent");            
            var rent = (obj.RentDate - start).TotalMilliseconds;

            // Queing some async operation
            await Task.Delay(100);

            var beforeReturn = DateTime.UtcNow;
            pool.Return(obj);
            var afterReturn = (DateTime.UtcNow - beforeReturn).TotalMilliseconds;

            //Console.WriteLine($"Done. Rent took {rent}ms, return took {afterReturn}ms");
        }
        */
    }
}
