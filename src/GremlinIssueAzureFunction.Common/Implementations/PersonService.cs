using System;
using System.Threading.Tasks;
using ExRam.Gremlinq.Core;
using Gremlin.Net.Driver.Exceptions;
using GremlinIssueAzureFunction.Common.Interfaces;
using GremlinIssueAzureFunction.Common.Model;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace GremlinIssueAzureFunction.Common.Implementations
{
    public class PersonService : IPersonService
    {
        private readonly IGremlinQuerySource _g;
        private readonly IPartitionKeyHelper _partitionKeyHelper;
        private readonly ILogger<PersonService> _logger;

        public PersonService(IGremlinQuery gremlinQuery,
            ILogger<PersonService> logger,
            IPartitionKeyHelper partitionKeyHelper)
        {
            _logger = logger;
            _partitionKeyHelper = partitionKeyHelper;
            _g = gremlinQuery.GetGremlinQuerySource();
        }

        public async Task Add()
        {
            var id = Guid.NewGuid().ToString("N");
            var bucketNo = _partitionKeyHelper.GetBucketFromRecordId(id);
            var uctNow = DateTime.UtcNow;
            var person = new Person()
            {
                Id = id,
                BucketNo = bucketNo,
                FirstName = "First Name " + Guid.NewGuid(),
                LastName = "First Name " + Guid.NewGuid(),
                LastUpdatedDateTime = uctNow,
                CreationDateTime = uctNow,
                CreatorId = id
            };
            try
            {
                await _g.AddV<Person>(person);
            }
            catch (ResponseException exception)
            {
                if (exception.StatusAttributes.TryGetValue("x-ms-status-code").Exists(x => (long) x == 429))
                {
                    _logger.LogInformation($"x-ms-status-code => 429");
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
