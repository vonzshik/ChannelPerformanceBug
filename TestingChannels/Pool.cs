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
            for (var i = 0; i < objects.Length; i++)
            {
                objects[i] = new PooledObject();
                writer.TryWrite(objects[i]);
            }
        }

        public async ValueTask<PooledObject> Rent(bool async)
        {
            if (reader.TryRead(out var readPooledObject))
                return readPooledObject;

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
