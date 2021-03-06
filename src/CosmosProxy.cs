﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;

namespace Acamti.Azure.Cosmos
{
    public class CosmosProxy : ICosmosProxy
    {
        private readonly Container _container;

        public CosmosProxy(IOptions<CosmosProxyConfiguration> config, CustomCosmosClientOptions clientOptions)
        {
            var client = new CosmosClient(config.Value.ConnectionString, clientOptions.CosmosClientOptions);

            _container = client.GetContainer(config.Value.DatabaseId, config.Value.ContainerId);
        }

        public Task<ItemResponse<TDocument>> CreateDocumentAsync<TDocument>(TDocument document,
            PartitionKey partitionKey,
            ItemRequestOptions requestOptions = null,
            CancellationToken cancellationToken = default)
            where TDocument : class =>
            _container.CreateItemAsync(
                document,
                partitionKey,
                requestOptions,
                cancellationToken
            );

        public Task<ItemResponse<TDocument>> ReplaceDocumentAsync<TDocument>(TDocument document,
            string documentId,
            PartitionKey partitionKey,
            ItemRequestOptions requestOptions = null,
            CancellationToken cancellationToken = default)
            where TDocument : class =>
            _container.ReplaceItemAsync(
                document,
                documentId,
                partitionKey,
                requestOptions,
                cancellationToken
            );

        public Task<ItemResponse<TDocument>> UpsertDocumentAsync<TDocument>(TDocument document,
            PartitionKey partitionKey,
            ItemRequestOptions requestOptions = null,
            CancellationToken cancellationToken = default)
            where TDocument : class =>
            _container.UpsertItemAsync(
                document,
                partitionKey,
                requestOptions,
                cancellationToken
            );

        public Task<ItemResponse<TDocument>> GetDocumentAsync<TDocument>(string id,
            PartitionKey partitionKey,
            ItemRequestOptions requestOptions = null)
            where TDocument : class => _container.ReadItemAsync<TDocument>(id, partitionKey, requestOptions);

        public async Task<IEnumerable<TDocument>> GetDocumentsAsync<TDocument>(Func<IQueryable<TDocument>, IQueryable<TDocument>> conditionBuilder = null,
            QueryRequestOptions requestOptions = null)
            where TDocument : class
        {
            var docList = new List<TDocument>();

            FeedIterator<TDocument> feedIterator = (
                conditionBuilder is null
                    ? _container.GetItemLinqQueryable<TDocument>(requestOptions: requestOptions)
                    : conditionBuilder(_container.GetItemLinqQueryable<TDocument>(requestOptions: requestOptions))
            ).ToFeedIterator();

            while (feedIterator.HasMoreResults)
                docList.AddRange(await feedIterator.ReadNextAsync());

            return docList;
        }

        public async IAsyncEnumerable<TDocument> GetDocumentsIteratorAsync<TDocument>(Func<IQueryable<TDocument>, IQueryable<TDocument>> conditionBuilder = null,
            QueryRequestOptions requestOptions = null)
            where TDocument : class
        {
            FeedIterator<TDocument> feedIterator = (conditionBuilder is null
                    ? _container.GetItemLinqQueryable<TDocument>(requestOptions: requestOptions)
                    : conditionBuilder(_container.GetItemLinqQueryable<TDocument>(requestOptions: requestOptions))
                ).ToFeedIterator();

            while (feedIterator.HasMoreResults)
            {
                foreach (TDocument doc in await feedIterator.ReadNextAsync())
                    yield return doc;
            }
        }
    }
}
