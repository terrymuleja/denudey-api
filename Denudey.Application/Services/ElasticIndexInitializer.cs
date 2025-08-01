using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Microsoft.Extensions.Logging;

namespace Denudey.Application.Services
{
    public class ElasticIndexInitializer
    {
        private readonly ElasticsearchClient _elasticClient;
        private readonly ILogger<ElasticIndexInitializer> _logger;
        private const string EpisodesIndexName = "scamflix_episodes";

        public ElasticIndexInitializer(ElasticsearchClient elasticClient, ILogger<ElasticIndexInitializer> logger)
        {
            _elasticClient = elasticClient;
            _logger = logger;
        }

        public async Task CreateEpisodesIndexAsync()
        {
            try
            {
                _logger.LogInformation("Checking if index '{IndexName}' exists...", EpisodesIndexName);

                // Check if index exists
                var existsResponse = await _elasticClient.Indices.ExistsAsync(EpisodesIndexName);

                if (existsResponse.Exists)
                {
                    _logger.LogInformation("Index '{IndexName}' already exists", EpisodesIndexName);
                    return;
                }

                _logger.LogInformation("Creating index '{IndexName}'...", EpisodesIndexName);

                // Create the index with basic mapping - let Elasticsearch auto-detect most fields
                var createResponse = await _elasticClient.Indices.CreateAsync(EpisodesIndexName, c => c
                    .Settings(s => s
                        .NumberOfShards(1)
                        .NumberOfReplicas(0)
                    )
                );

                if (createResponse.IsValidResponse)
                {
                    _logger.LogInformation("Successfully created index '{IndexName}'", EpisodesIndexName);
                }
                else
                {
                    _logger.LogError("Failed to create index '{IndexName}': {Error}",
                        EpisodesIndexName, createResponse.DebugInformation);
                    throw new Exception($"Failed to create Elasticsearch index: {createResponse.DebugInformation}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Elasticsearch index '{IndexName}'", EpisodesIndexName);
                throw;
            }
        }

        public async Task DeleteIndexAsync()
        {
            try
            {
                _logger.LogInformation("Deleting index '{IndexName}'...", EpisodesIndexName);

                var deleteResponse = await _elasticClient.Indices.DeleteAsync(EpisodesIndexName);

                if (deleteResponse.IsValidResponse)
                {
                    _logger.LogInformation("Successfully deleted index '{IndexName}'", EpisodesIndexName);
                }
                else
                {
                    _logger.LogWarning("Failed to delete index '{IndexName}': {Error}",
                        EpisodesIndexName, deleteResponse.DebugInformation);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Elasticsearch index '{IndexName}'", EpisodesIndexName);
                throw;
            }
        }

        public async Task RecreateIndexAsync()
        {
            try
            {
                _logger.LogInformation("Recreating index '{IndexName}'...", EpisodesIndexName);

                await DeleteIndexAsync();
                await CreateEpisodesIndexAsync();

                _logger.LogInformation("Successfully recreated index '{IndexName}'", EpisodesIndexName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recreating Elasticsearch index '{IndexName}'", EpisodesIndexName);
                throw;
            }
        }

        public async Task<bool> IndexExistsAsync()
        {
            try
            {
                var response = await _elasticClient.Indices.ExistsAsync(EpisodesIndexName);
                return response.Exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if index '{IndexName}' exists", EpisodesIndexName);
                return false;
            }
        }
    }
}