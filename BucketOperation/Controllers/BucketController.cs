using System.Reactive.Linq;
using BucketOperation.Dtos;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.ApiEndpoints;
using Minio.DataModel;
using Minio.DataModel.Args;

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
        var buckets = await _minioClient.ListBucketsAsync();
        return Ok(buckets);
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
                .WithBucket(dto.Name);
            await _minioClient.MakeBucketAsync(makeBucketArgs);
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

}