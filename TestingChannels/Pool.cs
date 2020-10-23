using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Npgsql;

namespace TestingChannels
{
    class Pool
    {
        static readonly SingleThreadSynchronizationContext SingleThreadSynchronizationContext = new SingleThreadSynchronizationContext("NpgsqlRemainingAsyncSendWorker");

        readonly ChannelReader<PooledObject> reader;
        readonly ChannelWriter<PooledObject> writer;

        readonly PooledObject[] objects;

        volatile int _numObjects;

        public Pool()
        {
            var channel = Channel.CreateUnbounded<PooledObject>();

            reader = channel.Reader;
            writer = channel.Writer;

            objects = new PooledObject[100];
        }

        public async ValueTask<PooledObject> Rent(bool async)
        {
            if (reader.TryRead(out var readPooledObject))
                return readPooledObject;

            for (var numObjects = _numObjects; numObjects < 100; numObjects = _numObjects)
            {
                if (Interlocked.CompareExchange(ref _numObjects, numObjects + 1, numObjects) != numObjects)
                    continue;

                var pooledObject = new PooledObject();

                var i = 0;
                for (; i < 100; i++)
                    if (Interlocked.CompareExchange(ref objects[i], pooledObject, null) == null)
                        break;
                if (i == 100)
                    throw new Exception($"Some exception");

                return pooledObject;
            }

            if (async)
                return await reader.ReadAsync();

            //using (SingleThreadSynchronizationContext.Enter())
            return reader.ReadAsync().AsTask().GetAwaiter().GetResult();
        }

        public void Return(PooledObject pooledObject)
        {
            pooledObject.ReturnDate = DateTime.UtcNow;
            writer.TryWrite(pooledObject);
        }
    }
}
