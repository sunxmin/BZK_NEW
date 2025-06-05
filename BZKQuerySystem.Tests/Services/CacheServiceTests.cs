using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BZKQuerySystem.Services;
using Moq;

namespace BZKQuerySystem.Tests.Services
{
    /// <summary>
    /// 缓存服务单元测试 - 第一阶段优化
    /// </summary>
    public class CacheServiceTests : IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private readonly Mock<ILogger<CacheService>> _loggerMock;
        private readonly CacheService _cacheService;

        public CacheServiceTests()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _loggerMock = new Mock<ILogger<CacheService>>();
            
            _cacheService = new CacheService(_memoryCache, _loggerMock.Object);
        }

        [Fact]
        public async Task SetAsync_ShouldCacheValue_WithDefaultExpiration()
        {
            // Arrange
            var key = "test_key";
            var value = "test_value";

            // Act
            await _cacheService.SetAsync(key, value);
            var cachedValue = await _cacheService.GetAsync<string>(key);

            // Assert
            Assert.Equal(value, cachedValue);
        }

        [Fact]
        public async Task SetAsync_ShouldCacheValue_WithCustomExpiration()
        {
            // Arrange
            var key = "test_key_custom";
            var value = "test_value_custom";
            var expiration = TimeSpan.FromMinutes(10);

            // Act
            await _cacheService.SetAsync(key, value, expiration);
            var cachedValue = await _cacheService.GetAsync<string>(key);

            // Assert
            Assert.Equal(value, cachedValue);
        }

        [Fact]
        public async Task GetAsync_ShouldReturnNull_WhenKeyNotExists()
        {
            // Arrange
            var key = "non_existent_key";

            // Act
            var cachedValue = await _cacheService.GetAsync<string>(key);

            // Assert
            Assert.Null(cachedValue);
        }

        [Fact]
        public async Task RemoveAsync_ShouldRemoveCachedValue()
        {
            // Arrange
            var key = "remove_test_key";
            var value = "remove_test_value";
            await _cacheService.SetAsync(key, value);

            // Act
            await _cacheService.RemoveAsync(key);
            var cachedValue = await _cacheService.GetAsync<string>(key);

            // Assert
            Assert.Null(cachedValue);
        }

        [Theory]
        [InlineData("string_value")]
        [InlineData(123)]
        [InlineData(true)]
        public async Task SetAndGet_ShouldWorkWithDifferentTypes<T>(T value)
        {
            // Arrange
            var key = $"type_test_{typeof(T).Name}";

            // Act
            await _cacheService.SetAsync(key, value);
            var cachedValue = await _cacheService.GetAsync<T>(key);

            // Assert
            Assert.Equal(value, cachedValue);
        }

        [Fact]
        public async Task SetAsync_ShouldHandleComplexObjects()
        {
            // Arrange
            var key = "complex_object";
            var value = new TestObject 
            { 
                Id = 1, 
                Name = "Test", 
                CreatedAt = DateTime.Now 
            };

            // Act
            await _cacheService.SetAsync(key, value);
            var cachedValue = await _cacheService.GetAsync<TestObject>(key);

            // Assert
            Assert.NotNull(cachedValue);
            Assert.Equal(value.Id, cachedValue.Id);
            Assert.Equal(value.Name, cachedValue.Name);
        }

        [Fact]
        public void GenerateCacheKey_ShouldGenerateConsistentKeys()
        {
            // Arrange
            var keyParts = new object[] { "user", 123, "query" };

            // Act
            var key1 = _cacheService.GenerateCacheKey(keyParts);
            var key2 = _cacheService.GenerateCacheKey(keyParts);

            // Assert
            Assert.Equal(key1, key2);
            Assert.NotEmpty(key1);
        }

        public void Dispose()
        {
            _memoryCache.Dispose();
        }

        private class TestObject
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }
    }
} 