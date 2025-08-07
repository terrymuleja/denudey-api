using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Microsoft.Extensions.Logging;

namespace Denudey.Application.Services
{
    public class ElasticProductIndexInitializer
    {
        private readonly ElasticsearchClient _elasticClient;
        private readonly ILogger<ElasticProductIndexInitializer> _logger;
        private const string IndexName = "products";

        public ElasticProductIndexInitializer(ElasticsearchClient elasticClient, ILogger<ElasticProductIndexInitializer> logger)
        {
            _elasticClient = elasticClient;
            _logger = logger;
        }

        public async Task CreateIndexAsync()
        {
            try
            {
                _logger.LogInformation("Checking if index '{IndexName}' exists...", IndexName);

                // Check if index exists
                var existsResponse = await _elasticClient.Indices.ExistsAsync(IndexName);

                if (existsResponse.Exists)
                {
                    _logger.LogInformation("Index '{IndexName}' already exists", IndexName);
                    return;
                }

                _logger.LogInformation("Creating index '{IndexName}'...", IndexName);

                // Create the index with basic mapping - let Elasticsearch auto-detect most fields
                var createResponse = await _elasticClient.Indices.CreateAsync(IndexName, c => c
                    .Settings(s => s
                        .NumberOfShards(1)
                        .NumberOfReplicas(0)
                    )
                );

                if (createResponse.IsValidResponse)
                {
                    _logger.LogInformation("Successfully created index '{IndexName}'", IndexName);
                }
                else
                {
                    _logger.LogError("Failed to create index '{IndexName}': {Error}",
                        IndexName, createResponse.DebugInformation);
                    throw new Exception($"Failed to create Elasticsearch index: {createResponse.DebugInformation}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Elasticsearch index '{IndexName}'", IndexName);
                throw;
            }
        }

        public async Task DeleteIndexAsync()
        {
            try
            {
                _logger.LogInformation("Deleting index '{IndexName}'...", IndexName);

                var deleteResponse = await _elasticClient.Indices.DeleteAsync(IndexName);

                if (deleteResponse.IsValidResponse)
                {
                    _logger.LogInformation("Successfully deleted index '{IndexName}'", IndexName);
                }
                else
                {
                    _logger.LogWarning("Failed to delete index '{IndexName}': {Error}",
                        IndexName, deleteResponse.DebugInformation);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Elasticsearch index '{IndexName}'", IndexName);
                throw;
            }
        }

        public async Task RecreateIndexAsync()
        {
            try
            {
                _logger.LogInformation("Recreating index '{IndexName}'...", IndexName);

                await DeleteIndexAsync();
                await CreateIndexAsync();

                _logger.LogInformation("Successfully recreated index '{IndexName}'", IndexName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recreating Elasticsearch index '{IndexName}'", IndexName);
                throw;
            }
        }
        
        public async Task<bool> IndexExistsAsync()
        {
            try
            {
                var response = await _elasticClient.Indices.ExistsAsync(IndexName);
                return response.Exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if index '{IndexName}' exists", IndexName);
                return false;
            }
        }
    }
}