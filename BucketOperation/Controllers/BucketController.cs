using System.Reactive.Linq;
using BucketOperation.Dtos;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.ApiEndpoints;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.ILM;
using Minio.DataModel.ObjectLock;
using Minio.Exceptions;

namespace BucketOperation.Controllers;

[ApiController]
[Route("[controller]")]
public class BucketController : ControllerBase
{
    private readonly IMinioClient _minioClient;
    
    public BucketController(IMinioClient minioClient)
    {
        _minioClient = minioClient;
    }
    
    [HttpGet("GetBuckets")]
    public async Task<IActionResult> GetBuckets()
    {
        try
        {
            var buckets = await _minioClient.ListBucketsAsync();
            return Ok(buckets);
        }
        catch (AuthorizationException e)
        {
            return BadRequest("alsdkj");
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    
    }

    [HttpPost("CreateBucket")]
    public async Task<IActionResult> CreateBucket([FromBody] CreateBucketDto dto)
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(dto.Name);
            if (await _minioClient.BucketExistsAsync(bucketExistsArgs))
            {
                return BadRequest("Bucket is exists");
            }

            
            var makeBucketArgs = new MakeBucketArgs()
                .WithBucket(dto.Name)
                .WithObjectLock();
            await _minioClient.MakeBucketAsync(makeBucketArgs);
            if (dto.Versioning)
            {
                var setVersioningArgs = new SetVersioningArgs()
                    .WithBucket(dto.Name)
                    .WithVersioningEnabled();
                await _minioClient.SetVersioningAsync(setVersioningArgs);
            }

            if (dto.ObjectLocking)
            {
                ObjectLockConfiguration objectLockConfiguration = null;
                if (dto.Mode == null || dto.NumOfDays == null)
                {
                    objectLockConfiguration = new ObjectLockConfiguration();
                }
                else
                {
                    objectLockConfiguration = new ObjectLockConfiguration(dto.Mode ?? ObjectRetentionMode.COMPLIANCE, dto.NumOfDays ?? 365);
                }
                 
                var setObjectLockConfigurationArgs = new SetObjectLockConfigurationArgs()
                    .WithBucket(dto.Name)
                    .WithLockConfiguration(objectLockConfiguration);
                await _minioClient.SetObjectLockConfigurationAsync(setObjectLockConfigurationArgs);
            }

            var lifecycleRules = new List<LifecycleRule>()
            {
                new LifecycleRule()
                {
                    ID = "ExpireObjectsAfter5Minutes",
                    Status = LifecycleRule.LifecycleRuleStatusEnabled,
                    Expiration = new Expiration()
                    {
                        ExpiryDate = $"{new DateTimeOffset(DateTime.UtcNow.AddMinutes(2)).ToUnixTimeSeconds()}",
                    },
                    Filter = new RuleFilter()
                    {
                        Prefix = ""
                    }
                },
            };
            var lifecycleConfiguration = new LifecycleConfiguration(lifecycleRules);
            var setBucketLifecycleArgs = new SetBucketLifecycleArgs()
                .WithBucket(dto.Name)
                .WithLifecycleConfiguration(lifecycleConfiguration);
            await _minioClient.SetBucketLifecycleAsync(setBucketLifecycleArgs);
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
    [HttpGet("BucketExists")]
    public async Task<IActionResult> BucketExists([FromQuery] string bucketName)
    {
        var bucketExistsArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        var result = await _minioClient.BucketExistsAsync(bucketExistsArgs);
        return Ok(result);
    }
    
    [HttpDelete("RemoveBucket")]
    public async Task<IActionResult> RemoveBucket([FromQuery] string bucketName)
    {
        try
        {
            var removeBucketArgs = new RemoveBucketArgs()
                .WithBucket(bucketName);
            await _minioClient.RemoveBucketAsync(removeBucketArgs);
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
    [HttpGet("GetListObjects")]
    public async Task<IActionResult> GetListObjects([FromQuery] string bucketName)
    {
        try
        {
            var listObjectsArgs = new ListObjectsArgs()
                .WithBucket(bucketName);
            List<string> objects = new List<string>();
            var result = _minioClient.ListObjectsEnumAsync(listObjectsArgs).ConfigureAwait(false);
            await foreach (var item in result)
            {
                objects.Add(item.Key);
            }
            return Ok(objects);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
    [HttpGet("GetLatestObjects")]    
    public async Task<IActionResult> GetLatestObjects([FromQuery] string? bucketName)
    {
        try
        {
            if (bucketName == null)
            {
                var buckets = await _minioClient.ListBucketsAsync();
                Item? objectMinio = null;
                foreach (var bucket in buckets.Buckets)
                {
                    var listObjectsArgs = new ListObjectsArgs()
                        .WithBucket(bucket.Name);
                 
                    var result = _minioClient.ListObjectsEnumAsync(listObjectsArgs).ConfigureAwait(false);
                    await foreach (var item in result)
                    {
                        if (objectMinio == null)
                        {
                            objectMinio = item;
                        }
                        else if (objectMinio.LastModifiedDateTime < item.LastModifiedDateTime)
                        {
                            objectMinio = item;
                        }
                    }
                }
                return Ok(objectMinio);
            }
            else
            {
                var listObjectsArgs = new ListObjectsArgs()
                    .WithBucket(bucketName);
                Item? objectMinio = null;
                var result = _minioClient.ListObjectsEnumAsync(listObjectsArgs).ConfigureAwait(false);
                await foreach (var item in result)
                {
                    if (objectMinio == null)
                    {
                        objectMinio = item;
                    }
                    else if (objectMinio.LastModifiedDateTime < item.LastModifiedDateTime)
                    {
                        objectMinio = item;
                    }
                }
                return Ok(objectMinio);
            }
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
    [HttpGet("ListIncompleteUploads")]
    public async Task<IActionResult> ListIncompleteUploads([FromQuery] string bucketName)
    {
        try
        {
            var listIncompleteUploads = new ListIncompleteUploadsArgs()
                .WithBucket(bucketName);
            var result = _minioClient
                .ListIncompleteUploadsEnumAsync(listIncompleteUploads)
                .ConfigureAwait(false);
            var uploads = new List<Upload>();
            await foreach (var upload in result)
            {
                uploads.Add(upload);
            }
            return Ok(uploads);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
    [HttpGet("GetBucketPolicy")]
    public async Task<IActionResult> GetBucketPolicy([FromQuery] string bucketName)
    {
        try
        {
            var getPolicyArgs = new GetPolicyArgs()
                .WithBucket(bucketName);
            var policies = await _minioClient.GetPolicyAsync(getPolicyArgs);
            return Ok(policies);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
    [HttpPost("SetBucketPolicy")]
    public async Task<IActionResult> SetBucketPolicy([FromBody] SetBucketPolicyDto dto)
    {
        try
        {
            var setPolicyArgs = new SetPolicyArgs()
                .WithBucket(dto.BucketName)
                .WithPolicy(dto.Policy);
            await _minioClient.SetPolicyAsync(setPolicyArgs);
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    [HttpGet("GetVersioning")]
    public async Task<IActionResult> GetVersioning([FromQuery] string bucketName)
    {
        try
        {
            var getVersingArgs = new GetVersioningArgs()
                .WithBucket(bucketName);
            var result = await _minioClient.GetVersioningAsync(getVersingArgs);
            return Ok(result);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}