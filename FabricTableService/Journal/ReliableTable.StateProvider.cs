﻿// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   The distributed journal.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace FabricTableService.Journal
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Fabric;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using global::FabricTableService.Journal.Database;

    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Replicator;

    using Transaction = Microsoft.ServiceFabric.Replicator.Transaction;
    using TransactionBase = Microsoft.ServiceFabric.Replicator.TransactionBase;

    /// <summary>
    /// The distributed journal.
    /// </summary>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    public partial class ReliableTable<TKey, TValue> : IStateProvider2, IReliableState
    {
        /// <summary>
        /// The state replicator.
        /// </summary>
        private TransactionalReplicator replicator;

        /// <summary>
        /// The partition id.
        /// </summary>
        private string partitionId;

        /// <summary>
        /// Gets the name of this provider.
        /// </summary>
        public Uri Name { get; private set; }

        /// <summary>
        /// Gets the initialization context.
        /// </summary>
        public byte[] InitializationContext { get; private set; }

        /// <summary>
        /// The initialize.
        /// </summary>
        /// <param name="transactionalReplicator">
        /// The transactional replicator.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="initializationContext">
        /// The initialization context.
        /// </param>
        /// <param name="stateProviderId">
        /// The state provider id.
        /// </param>
        void IStateProvider2.Initialize(
            TransactionalReplicator transactionalReplicator,
            Uri name,
            byte[] initializationContext,
            Guid stateProviderId)
        {
            this.replicator = transactionalReplicator;
            this.partitionId = this.replicator.StatefulPartition.PartitionInfo.Id.ToString("N");
            this.InitializationContext = initializationContext;
            this.Name = name;
            var contextString = initializationContext == null
                                    ? string.Empty
                                    : string.Concat(initializationContext.Select(_ => $"{_:X}"));
            Trace.TraceInformation(
                "[" + this.partitionId + "] " + "Initialize({0}, {1}, {2})",
                name,
                contextString,
                stateProviderId);
        }

        /// <summary>
        /// The open async.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task IStateProvider2.OpenAsync()
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "OpenAsync()");

            var tableName = this.GetTableName();
            var workDirectory = this.replicator.InitializationParameters.CodePackageActivationContext.WorkDirectory;
            var databaseDirectory = Path.Combine(workDirectory, this.partitionId, "journal");
            Directory.CreateDirectory(databaseDirectory);
            this.tables = new PersistentTablePool<TKey, TValue>(databaseDirectory, "db.edb", tableName);
            this.tables.Initialize();
            return Task.FromResult(0);
        }

        /// <summary>
        /// Gets the table name.
        /// </summary>
        /// <returns></returns>
        private string GetTableName()
        {
            var tableName = this.Name.ToString().Substring(this.Name.Scheme.Length);
            return Regex.Replace(tableName, @"[^a-zA-Z0-9_\-]", string.Empty);
        }

        /// <summary>
        /// The close async.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task IStateProvider2.CloseAsync()
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "CloseAsync()");
            var tab = this.tables;
            if (tab != null)
            {
                ((IDisposable)tab).Dispose();
                this.tables = null;
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// The abort.
        /// </summary>
        void IStateProvider2.Abort()
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "Abort()");
        }

        /// <summary>
        /// The change role async.
        /// </summary>
        /// <param name="newRole">
        /// The new role.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task IStateProvider2.ChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "ChangeRoleAsync({0})", newRole);
            return Task.FromResult(0);
        }

        /// <summary>
        /// The on data loss async.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task IStateProvider2.OnDataLossAsync()
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "OnDataLossAsync()");
            return Task.FromResult(0);
        }

        /// <summary>
        /// The prepare checkpoint async.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task IStateProvider2.PrepareCheckpointAsync(CancellationToken cancellationToken)
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "PrepareCheckpointAsync()");
            return Task.FromResult(0);
        }

        /// <summary>
        /// The perform checkpoint async.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task IStateProvider2.PerformCheckpointAsync(CancellationToken cancellationToken)
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "PerformCheckpointAsync()");
            return Task.FromResult(0);
        }

        /// <summary>
        /// The complete checkpoint async.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task IStateProvider2.CompleteCheckpointAsync(CancellationToken cancellationToken)
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "CompleteCheckpointAsync()");
            return Task.FromResult(0);
        }

        /// <summary>
        /// The recover checkpoint async.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task IStateProvider2.RecoverCheckpointAsync()
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "RecoverCheckpointAsync()");
            return Task.FromResult(0);
        }

        /// <summary>
        /// The on recovery completed async.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task IStateProvider2.OnRecoveryCompletedAsync()
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "OnRecoveryCompletedAsync()");
            return Task.FromResult(0);
        }

        /// <summary>
        /// The backup checkpoint async.
        /// </summary>
        /// <param name="backupDirectory">
        /// The backup directory.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task IStateProvider2.BackupCheckpointAsync(string backupDirectory, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "BackupCheckpointAsync({0})", backupDirectory);
            return Task.FromResult(0);
        }

        /// <summary>
        /// The restore checkpoint async.
        /// </summary>
        /// <param name="backupDirectory">
        /// The backup directory.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task IStateProvider2.RestoreCheckpointAsync(string backupDirectory, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "RestoreCheckpointAsync({0})", backupDirectory);
            return Task.FromResult(0);
        }

        /// <summary>
        /// The get current state.
        /// </summary>
        /// <returns>
        /// The <see cref="IOperationDataStream"/>.
        /// </returns>
        IOperationDataStream IStateProvider2.GetCurrentState()
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "GetCurrentState()");
            return new CopyStream(this.tables);
        }

        /// <summary>
        /// The begin setting current state.
        /// </summary>
        void IStateProvider2.BeginSettingCurrentState()
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "BeginSettingCurrentState()");
        }

        /// <summary>
        /// The set current state.
        /// </summary>
        /// <param name="stateRecordNumber">
        /// The state record number.
        /// </param>
        /// <param name="data">
        /// The data.
        /// </param>
        Task IStateProvider2.SetCurrentStateAsync(long stateRecordNumber, OperationData data)
        {
            var count = data?.Count ?? 0;
            var length = data?.Sum(_ => _.Count) ?? 0;
            Trace.TraceInformation(
                "[" + this.partitionId + "] " + "SetCurrentState({0}, [{1} operations, {2}b])",
                stateRecordNumber,
                count,
                length);

            /*
             * indexex: LSN, row key + partition key
             */

            return Task.FromResult(0);
        }

        /// <summary>
        /// The end setting current state.
        /// </summary>
        void IStateProvider2.EndSettingCurrentState()
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "EndSettingCurrentState()");
        }

        /// <summary>
        /// The prepare for remove async.
        /// </summary>
        /// <param name="transaction">
        /// The transaction.
        /// </param>
        /// <param name="timeout">
        /// The timeout.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task IStateProvider2.PrepareForRemoveAsync(
            Transaction transaction,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "PrepareForRemoveAsync()");
            return Task.FromResult(0);
        }

        /// <summary>
        /// The remove state async.
        /// </summary>
        /// <param name="stateProviderId">
        /// The state provider id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task IStateProvider2.RemoveStateAsync(Guid stateProviderId)
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "RemoveStateAsync({0})", stateProviderId);
            return Task.FromResult(0);
        }

        /// <summary>
        /// The apply async.
        /// </summary>
        /// <param name="lsn">
        /// The lsn.
        /// </param>
        /// <param name="transactionBase">
        /// The transaction base.
        /// </param>
        /// <param name="data">
        /// The data.
        /// </param>
        /// <param name="applyContext">
        /// The apply context.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task<object> IStateProvider2.ApplyAsync(
            long lsn,
            TransactionBase transactionBase,
            OperationData data,
            ApplyContext applyContext)
        {
            // Get the operation.
            var operation = Operation.Deserialize(data[0]);

            // Resume the existing transaction for this operation or start a transaction for this operation.
            bool applied;
            OperationContext context;
            DatabaseTransaction<TKey, TValue> tx;
            if (IsPrimaryOperation(applyContext) && this.inProgressOperations.TryGetValue(operation.Id, out context))
            {
                applied = true;
                tx = context.DatabaseTransaction;
                tx.Resume();
            }
            else
            {
                // The operation has not yet been applied and therefore a transaction has not been initiated.
                applied = false;
                tx = this.tables.CreateTransaction();
            }

            /*var part = this.replicator.StatefulPartition;
            var operationString = JsonConvert.SerializeObject(operation, SerializationSettings.JsonConfig);
            Trace.TraceInformation(
                $"[{this.partitionId}/{this.replicator.InitializationParameters.ReplicaId} r:{part.ReadStatus} w:{part.WriteStatus}] ApplyAsync(lsn: {lsn}, tx: {transactionBase.Id}, op: {operationString} (length: {data?.Length ?? 0}), context: {applyContext})");
            */
            try
            {
                // If the operation has not yet been applied, apply it.
                if (!applied)
                {
                    //Trace.TraceInformation($"{applyContext} Apply {operationString}");
                    operation.Apply(tx.Table);
                }

                //Trace.TraceInformation($"{applyContext} Commit {operationString}");
                tx.Commit();
            }
            catch (Exception exception)
            {
                tx.Rollback();

                return Task.FromException<object>(exception);
            }
            finally
            {
                if (IsPrimaryOperation(applyContext))
                {
                    this.inProgressOperations.TryRemove(operation.Id, out context);
                }

                tx.Dispose();
            }

            return Task.FromResult(default(object));
        }

        private T PerformOperation<T>(long id, OperationContext context, Operation undo, Operation redo)
        {
            T result;
            var tx = default(DatabaseTransaction<TKey, TValue>);
            var inFlight = false;
            try
            {
                tx = context.DatabaseTransaction;
                
                // Apply initially on primary, but do not commit.
                result = (T)redo.Apply(tx.Table);

                // Add the operation to the in-progress collection so that we can retrieve it when it is committed.
                inFlight = this.inProgressOperations.TryAdd(id, context);

                if (!inFlight)
                {
                    throw new InvalidOperationException($"Operation with id {id} already in-progress.");
                }

                // Add this operation to the transaction.
                context.ReplicatorTransaction.AddOperation(
                    new OperationData(undo.Serialize()),
                    new OperationData(redo.Serialize()),
                    null,
                    this.Name);
                tx.Pause();
            }
            catch
            {
                tx.Transaction?.Rollback();
                tx.Dispose();
                if (inFlight)
                {
                    this.inProgressOperations.TryRemove(id, out context);
                }

                throw;
            }

            return result;
        }

        private static bool IsPrimaryOperation(ApplyContext applyContext)
        {
            return ((int)applyContext & (int)ApplyContext.PRIMARY) == (int)ApplyContext.PRIMARY;
        }

        /// <summary>
        /// The unlock.
        /// </summary>
        /// <param name="state">
        /// The state.
        /// </param>
        void IStateProvider2.Unlock(object state)
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "Unlock()");
        }

        /// <summary>
        /// Returns the child state providers.
        /// </summary>
        /// <param name="name">
        /// The state provider name.
        /// </param>
        /// <returns>
        /// The child state providers.
        /// </returns>
        IEnumerable<IStateProvider2> IStateProvider2.GetChildren(Uri name)
        {
            Trace.TraceInformation("[" + this.partitionId + "] " + "GetChildren()");
            return Enumerable.Empty<IStateProvider2>();
        }

        /// <summary>
        /// Provides the stream of operations required to copy this store.
        /// </summary>
        internal class CopyStream : IOperationDataStream
        {
            /// <summary>
            /// The values.
            /// </summary>
            private readonly IEnumerator<KeyValuePair<TKey, TValue>> values;

            /// <summary>
            /// The pool of tables.
            /// </summary>
            private readonly PersistentTablePool<TKey, TValue> pool;

            /// <summary>
            /// The table.
            /// </summary>
            private PersistentTable<TKey, TValue> table;

            /// <summary>
            /// Initializes a new instance of the <see cref="CopyStream"/> class.
            /// </summary>
            /// <param name="pool">
            /// The table pool.
            /// </param>
            public CopyStream(PersistentTablePool<TKey, TValue> pool)
            {
                this.pool = pool;
                this.table = pool.Take();
                this.values = this.table.GetRange().GetEnumerator();
            }

            /// <summary>
            /// The get next async.
            /// </summary>
            /// <param name="cancellationToken">
            /// The cancellation token.
            /// </param>
            /// <returns>
            /// The <see cref="Task"/>.
            /// </returns>
            public Task<OperationData> GetNextAsync(CancellationToken cancellationToken)
            {
                if (this.values.MoveNext())
                {
                    var element = this.values.Current;
                    var data =
                        new OperationData(new SetOperation { Key = element.Key, Value = element.Value }.Serialize());
                    return Task.FromResult(data);
                }

                if (this.table == null)
                {
                    return Task.FromResult(default(OperationData));
                }

                this.pool.Return(this.table);
                this.table = null;

                return Task.FromResult(default(OperationData));
            }
        }

        /// <summary>
        /// Represents the context of an operation.
        /// </summary>
        internal struct OperationContext
        {
            /// <summary>
            /// Gets or sets the Service Fabric replicator transaction.
            /// </summary>
            public Transaction ReplicatorTransaction { get; set; }

            /// <summary>
            /// Gets or sets the database transaction.
            /// </summary>
            public DatabaseTransaction<TKey, TValue> DatabaseTransaction { get; set; }
        }
    }
}